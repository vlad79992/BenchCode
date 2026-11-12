using BenchCode.AuthService.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace BenchCode.AuthService.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(UserManager<User> userManager) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterUserRequest request)
    {
        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email
        };

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
        [Required, StringLength(100, MinimumLength = 3)] string UserName,
        [Required, EmailAddress] string Email,
        [Required, MinLength(8)] string Password);

    public sealed record UserResponse(string Id, string UserName, string Email);
}