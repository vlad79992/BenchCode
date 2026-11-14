using OpenIddict.Validation.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class OpenIddictServiceCollectionExtensions
{
    public static IServiceCollection AddOpenIddictTokenValidation(
        this IServiceCollection services,
        Uri issuer,
        string audience)
    {
        ArgumentNullException.ThrowIfNull(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);

        if (!issuer.IsAbsoluteUri)
        {
            throw new ArgumentException("The issuer must be an absolute URI.", nameof(issuer));
        }

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            });

        services.AddOpenIddict()
            .AddValidation(options =>
            {
                options.SetIssuer(issuer);
                options.AddAudiences(audience);
                options.UseSystemNetHttp();
                options.UseAspNetCore();
            });

        services.AddAuthorization();

        return services;
    }
}