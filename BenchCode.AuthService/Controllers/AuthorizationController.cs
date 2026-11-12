using BenchCode.AuthService.Entities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace BenchCode.AuthService.Controllers;

[ApiController]
[Route("connect")]
public class AuthorizationController(SignInManager<User> signInManager, UserManager<User> userManager) : ControllerBase
{
    private readonly SignInManager<User> signInManager = signInManager;
    private readonly UserManager<User> userManager = userManager;

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    [HttpPost("token")]
    public async Task<IActionResult> Exchange([FromForm] TokenRequest form)
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("Запрос OpenIddict не может быть получен.");

        if (request.IsPasswordGrantType())
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return InvalidGrant();
            }

            var user = await userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                return InvalidGrant();
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                return InvalidGrant();
            }

            var scopes = request.GetScopes();
            if (!scopes.Contains("benchcode-api", StringComparer.Ordinal))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidScope,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Требуется scope benchcode-api."
                    }));
            }

            var identity = new ClaimsIdentity(
                authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.AddClaim(Claims.Subject, await userManager.GetUserIdAsync(user));
            identity.AddClaim(Claims.Name, await userManager.GetUserNameAsync(user) ?? "unknown");

            var principal = new ClaimsPrincipal(identity);
            principal.SetDestinations(_ => [Destinations.AccessToken]);
            principal.SetScopes(scopes);
            principal.SetResources("benchcode-api");

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var subject = result.Principal?.GetClaim(Claims.Subject);
            var user = subject is null ? null : await userManager.FindByIdAsync(subject);

            if (!result.Succeeded || result.Principal is null || user is null || await userManager.IsLockedOutAsync(user))
            {
                return InvalidGrant();
            }

            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.UnsupportedGrantType,
            ErrorDescription = "Указанный grant type не поддерживается."
        });
    }

    private ForbidResult InvalidGrant() =>
        Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Неверное имя пользователя или пароль."
            }));

    public sealed class TokenRequest
    {
        [FromForm(Name = "grant_type")]
        public string? GrantType { get; set; }

        [FromForm(Name = "client_id")]
        public string? ClientId { get; set; }

        [FromForm(Name = "username")]
        public string? Username { get; set; }

        [FromForm(Name = "password")]
        public string? Password { get; set; }

        [FromForm(Name = "refresh_token")]
        public string? RefreshToken { get; set; }

        [FromForm(Name = "scope")]
        public string? Scope { get; set; }
    }
}