using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace BenchCode.Test;

public sealed class Fixture : IAsyncLifetime
{
    private const string PostgresImage = "postgres:19.0";

    private readonly PostgreSqlContainer authDbContainer =
        new PostgreSqlBuilder(PostgresImage).Build();
    private readonly PostgreSqlContainer competitionDbContainer =
        new PostgreSqlBuilder(PostgresImage).Build();


    public WebApplicationFactory<BenchCode.AuthService.Entities.User> AuthServiceFactory { get; private set; } = null!;
    public WebApplicationFactory<BenchCode.CompetitionService.Program> CompetitionFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {

        await authDbContainer.StartAsync();
        await competitionDbContainer.StartAsync();


        AuthServiceFactory = new WebApplicationFactory<BenchCode.AuthService.Entities.User>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("ConnectionStrings:authdb", authDbContainer.GetConnectionString());
                builder.UseSetting("OpenIddict:Issuer", "https://localhost:5001/");
            });

        CompetitionFactory = new WebApplicationFactory<BenchCode.CompetitionService.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting(
                    "ConnectionStrings:competitionsdb",
                    competitionDbContainer.GetConnectionString());
                builder.UseSetting("OpenIddict:Issuer", "https://localhost:5001/");
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthenticationHandler.Scheme;
                        options.DefaultChallengeScheme = TestAuthenticationHandler.Scheme;
                    }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationHandler.Scheme,
                        _ => { });
                });
            });
    }

    public async Task DisposeAsync()
    {
        await using var db = authDbContainer;
        await using var competitionDb = competitionDbContainer;
        await using var userFactory = AuthServiceFactory;
        await using var competitionFactory = CompetitionFactory;
    }
}