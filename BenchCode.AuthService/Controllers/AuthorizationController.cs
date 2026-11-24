using BenchCode.AuthService.Entities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.ComponentModel;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace BenchCode.AuthService.Controllers;

/// <summary>Обрабатывает запросы на получение и обновление токенов.</summary>
/// <param name="signInManager">Управляет входом пользователей.</param>
/// <param name="userManager">Управляет учетными записями пользователей.</param>
[ApiController]
[Route("authorization")]
public class AuthorizationController(SignInManager<User> signInManager, UserManager<User> userManager) : ControllerBase
{
    private readonly SignInManager<User> signInManager = signInManager;
    private readonly UserManager<User> userManager = userManager;

    /// <summary>Получить или обновить токен.</summary>
    /// <remarks>
    /// Для входа используйте `grant_type=password`; для обновления токена —
    /// `grant_type=refresh_token`.
    ///
    /// При входе нужны `username` и `password`. Поддерживаемые scope:
    /// `benchcode-api` и `offline_access` — последний нужен для получения
    /// refresh token.
    /// </remarks>
    /// <param name="form">Параметры запроса токена.</param>
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

    /// <summary>Параметры запроса для получения или обновления токена.</summary>
    public sealed class TokenRequest
    {
        /// <summary>Тип запроса: `password` или `refresh_token`.</summary>
        [Description("Тип запроса: password или refresh_token.")]
        [FromForm(Name = "grant_type")]
        public string? GrantType { get; set; }

        /// <summary>Идентификатор клиента, например `compose-web-app`.</summary>
        [Description("Идентификатор клиента, например compose-web-app.")]
        [FromForm(Name = "client_id")]
        public string? ClientId { get; set; }

        /// <summary>Имя пользователя; используется для `grant_type=password`.</summary>
        [Description("Имя пользователя; используется для grant_type=password.")]
        [FromForm(Name = "username")]
        public string? Username { get; set; }

        /// <summary>Пароль; используется для `grant_type=password`.</summary>
        [Description("Пароль; используется для grant_type=password.")]
        [FromForm(Name = "password")]
        public string? Password { get; set; }

        /// <summary>Refresh token; используется для `grant_type=refresh_token`.</summary>
        [Description("Refresh token; используется для grant_type=refresh_token.")]
        [FromForm(Name = "refresh_token")]
        public string? RefreshToken { get; set; }

        /// <summary>Запрашиваемые права: `benchcode-api`, `offline_access`.</summary>
        [Description("Запрашиваемые права: benchcode-api, offline_access.")]
        [FromForm(Name = "scope")]
        public string? Scope { get; set; }
    }
}