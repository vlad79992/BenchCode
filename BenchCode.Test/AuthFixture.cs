using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Testcontainers.PostgreSql;
using BenchCode.CompetitionService;

namespace BenchCode.Test;

public sealed class AuthFixture : IAsyncLifetime
{
    private const string connectionStringEnvironmentVariable = "ConnectionStrings__authdb";
    private const string competitionConnectionStringEnvironmentVariable = "ConnectionStrings__competitionsdb";

    private readonly PostgreSqlContainer authDbContainer =
        new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly PostgreSqlContainer competitionDbContainer =
        new PostgreSqlBuilder("postgres:16-alpine").Build();

    private string? previousConnectionString;
    private string? previousCompetitionConnectionString;

    public WebApplicationFactory<BenchCode.AuthService.Program> Factory { get; private set; } = null!;
    public WebApplicationFactory<BenchCode.CompetitionService.Program> CompetitionFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        previousConnectionString =
            Environment.GetEnvironmentVariable(connectionStringEnvironmentVariable);
        previousCompetitionConnectionString =
            Environment.GetEnvironmentVariable(competitionConnectionStringEnvironmentVariable);

        await authDbContainer.StartAsync();
        await competitionDbContainer.StartAsync();

        Environment.SetEnvironmentVariable(
            connectionStringEnvironmentVariable,
            authDbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable(
            competitionConnectionStringEnvironmentVariable,
            competitionDbContainer.GetConnectionString());

        Factory = new WebApplicationFactory<BenchCode.AuthService.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["OpenIddict:Issuer"] = "https://localhost:5001/"
                    });
                });
            });

        CompetitionFactory = new WebApplicationFactory<BenchCode.CompetitionService.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["OpenIddict:Issuer"] = "https://localhost:5001/"
                    });
                });
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
        try
        {
            if (Factory is not null)
            {
                await Factory.DisposeAsync();
            }
            if (CompetitionFactory is not null)
            {
                await CompetitionFactory.DisposeAsync();
            }
        }
        finally
        {
            try
            {
                await authDbContainer.DisposeAsync();
            }
            finally
            {
                try
                {
                    await competitionDbContainer.DisposeAsync();
                }
                finally
                {
                    Environment.SetEnvironmentVariable(
                        connectionStringEnvironmentVariable,
                        previousConnectionString);
                    Environment.SetEnvironmentVariable(
                        competitionConnectionStringEnvironmentVariable,
                        previousCompetitionConnectionString);
                }
            }
        }
    }
}