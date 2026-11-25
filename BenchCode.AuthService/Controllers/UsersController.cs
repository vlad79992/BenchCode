using BenchCode.AuthService.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace BenchCode.AuthService.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(UserManager<User> userManager, AuthDbContext dbContext) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterUserRequest request)
    {
        var hasProfile = request.FirstName is not null || request.LastName is not null || request.UserName is null;
        if (hasProfile && (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName)))
        {
            return BadRequest(new { errors = new[] { "Требуются имя и фамилия." } });
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => CreateUserAsync(request, hasProfile));
    }

    private async Task<ActionResult<UserResponse>> CreateUserAsync(RegisterUserRequest request, bool hasProfile)
    {
        dbContext.ChangeTracker.Clear();
        var user = new User
        {
            UserName = request.UserName ?? Guid.NewGuid().ToString("N"),
            Email = request.Email
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(HttpContext.RequestAborted);
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var duplicateErrors = result.Errors
                .Where(error => error.Code is "DuplicateUserName" or "DuplicateEmail")
                .Select(error => error.Description)
                .ToArray();

            if (duplicateErrors.Length > 0)
            {
                return Conflict(new { errors = duplicateErrors });
            }

            var errors = result.Errors
                .GroupBy(error => error.Code, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.Description).ToArray(),
                    StringComparer.Ordinal);

            return BadRequest(new ValidationProblemDetails(errors));
        }

        if (hasProfile)
        {
            var profile = await userManager.AddClaimsAsync(user, [
                new Claim(OpenIddictConstants.Claims.GivenName, request.FirstName!.Trim()),
                new Claim(OpenIddictConstants.Claims.FamilyName, request.LastName!.Trim())
            ]);
            if (!profile.Succeeded)
            {
                return Problem("Не удалось сохранить данные профиля.");
            }
        }

        await transaction.CommitAsync(HttpContext.RequestAborted);
        return CreatedAtAction(
            nameof(GetCurrentUser),
            new { },
            new UserResponse(user.Id, user.UserName, user.Email));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        var userId = User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId);
        return user is null
            ? Unauthorized()
            : Ok(new UserResponse(user.Id, user.UserName!, user.Email!));
    }

    public sealed record RegisterUserRequest(
        [StringLength(100, MinimumLength = 3)] string? UserName,
        [Required, EmailAddress] string Email,
        [Required, MinLength(8)] string Password,
        [StringLength(100)] string? FirstName = null,
        [StringLength(100)] string? LastName = null);

    public sealed record UserResponse(string Id, string UserName, string Email);
}