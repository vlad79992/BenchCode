using BenchCode.AuthService.Entities;
using BenchCode.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace BenchCode.AuthService.Controllers;

[ApiController]
[Route("users")]
public class UsersController(UserManager<User> userManager) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterUserRequest request)
    {
        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            FirstName = NormalizeName(request.FirstName),
            LastName = NormalizeName(request.LastName)
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
            new UserResponse(user.Id, user.UserName, user.FirstName, user.LastName, user.Email));
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
            : Ok(new UserResponse(user.Id, user.UserName, user.FirstName, user.LastName, user.Email));
    }

    /// <summary>Получить общедоступные профили пользователей по их ID.</summary>
    /// <param name="request">Список ID пользователей.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    [Authorize]
    [HttpPost("profiles/by-ids")]
    public async Task<ActionResult<IReadOnlyList<UserProfile>>> GetProfilesByIds(
        [FromBody] UserProfilesByIdsRequest request,
        CancellationToken cancellationToken)
    {
        var ids = request.UserIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (ids.Length == 0)
        {
            return Ok(Array.Empty<UserProfile>());
        }

        var profiles = await userManager.Users
            .Where(user => ids.Contains(user.Id))
            .Select(user => new UserProfile(user.Id, user.FirstName, user.LastName))
            .ToListAsync(cancellationToken);

        var profilesById = profiles.ToDictionary(profile => profile.Id, StringComparer.Ordinal);
        return Ok(ids.Where(profilesById.ContainsKey).Select(id => profilesById[id]).ToArray());
    }

    private static string? NormalizeName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Запрос пакетного поиска профилей по ID.</summary>
    /// <param name="UserIds">Не более 100 идентификаторов пользователей.</param>
    public sealed record UserProfilesByIdsRequest(
        [Required, MinLength(1), MaxLength(100)] string[] UserIds);

    public sealed record RegisterUserRequest(
        [Required, StringLength(100, MinimumLength = 3)] string UserName,
        [Required, EmailAddress] string Email,
        [Required, MinLength(8)] string Password,
        [MaxLength(100)] string? FirstName = null,
        [MaxLength(100)] string? LastName = null);

    public sealed record UserResponse(
        string Id,
        string? UserName,
        string? FirstName,
        string? LastName,
        string? Email) : UserProfile(Id, FirstName, LastName);
}