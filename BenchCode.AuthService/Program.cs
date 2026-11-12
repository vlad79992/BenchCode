using BenchCode.AuthService;
using BenchCode.AuthService.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Scalar.AspNetCore;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);
var issuer = builder.Configuration["OpenIddict:Issuer"];

// Add services to the container.

if (string.IsNullOrWhiteSpace(issuer) || !Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri))
{
    throw new InvalidOperationException("OpenIddict:Issuer must be configured as an absolute URI.");
}

var signingCertificate = builder.Environment.IsDevelopment()
    ? null
    : LoadCertificate(builder.Configuration, "Signing");
var encryptionCertificate = builder.Environment.IsDevelopment()
    ? null
    : LoadCertificate(builder.Configuration, "Encryption");

builder.AddNpgsqlDbContext<AuthDbContext>(connectionName: "authdb",
    configureDbContextOptions: opt =>
    {
        opt.UseOpenIddict();
    });

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

builder.Services.AddIdentityCore<User>(opt =>
    {
        opt.User.RequireUniqueEmail = true;
        opt.Password.RequiredLength = 8;
        opt.Lockout.AllowedForNewUsers = true;
        opt.Lockout.MaxFailedAccessAttempts = 5;
        opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddSignInManager<SignInManager<User>>();

builder.Services.AddOpenIddict()
    .AddCore(opt =>
    {
        opt.UseEntityFrameworkCore()
           .UseDbContext<AuthDbContext>();
    })
    .AddServer(opt =>
    {
        opt.SetIssuer(issuerUri);
        opt.SetTokenEndpointUris("/connect/token");

        opt.AllowPasswordFlow()
           .AllowRefreshTokenFlow();

        opt.RegisterScopes("benchcode-api");
        opt.SetAccessTokenLifetime(TimeSpan.FromMinutes(30));
        opt.SetRefreshTokenLifetime(TimeSpan.FromDays(14));

        if (signingCertificate is null || encryptionCertificate is null)
        {
            opt.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();
        }
        else
        {
            opt.AddSigningCertificate(signingCertificate)
               .AddEncryptionCertificate(encryptionCertificate);
        }

        opt.DisableAccessTokenEncryption();

        opt.UseAspNetCore()
           .EnableTokenEndpointPassthrough();
    })
    .AddValidation(opt =>
    {
        opt.UseLocalServer();
        opt.AddAudiences("benchcode-api");
        opt.UseAspNetCore();
    });

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    if (await applicationManager.FindByClientIdAsync("compose-web-app") is null)
    {
        await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "compose-web-app",
            DisplayName = "Compose Web App",
            ClientType = OpenIddictConstants.ClientTypes.Public,
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.Password,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.Prefixes.Scope + "benchcode-api",
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess
            }
        });
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static X509Certificate2 LoadCertificate(IConfiguration configuration, string name)
{
    var section = configuration.GetSection($"OpenIddict:Certificates:{name}");
    var path = section["Path"];
    var password = section["Password"];

    if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(password))
    {
        throw new InvalidOperationException(
            $"OpenIddict:Certificates:{name}:Path and Password must be configured outside Development.");
    }

    return X509CertificateLoader.LoadPkcs12FromFile(path, password);
}