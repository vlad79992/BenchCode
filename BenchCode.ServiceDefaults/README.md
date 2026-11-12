# OpenIddict token validation

Backend services can use the shared `AddOpenIddictTokenValidation` extension to validate AuthService access tokens without duplicating OpenIddict setup:

```csharp
var issuer = builder.Configuration.GetValue<Uri>("OpenIddict:Issuer")
    ?? throw new InvalidOperationException("OpenIddict:Issuer must be configured.");

builder.Services.AddOpenIddictTokenValidation(issuer, "benchcode-api");
```

Set `OpenIddict:Issuer` to the exact issuer URL configured by AuthService. The validator uses discovery and the issuer's signing keys, and checks the `benchcode-api` audience. Keep the token audience consistent with the resource registered by AuthService.

The extension configures authentication and authorization services. The application must also call `app.UseAuthentication()` before `app.UseAuthorization()` and apply `[Authorize]` to protected endpoints. For Aspire services, configure an issuer URL reachable by the validating service.
