using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace BenchCode.Test;

public sealed class AuthFixture : IAsyncLifetime
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__authdb";
    private const string CompetitionConnectionStringEnvironmentVariable =
        "ConnectionStrings__competitionsdb";
    private const string IssuerEnvironmentVariable = "OpenIddict__Issuer";

    private readonly PostgreSqlContainer dbContainer =
        new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly PostgreSqlContainer competitionDbContainer =
        new PostgreSqlBuilder("postgres:16-alpine").Build();

    private string? previousConnectionString;
    private string? previousCompetitionConnectionString;
    private string? previousIssuer;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public WebApplicationFactory<BenchCode.CompetitionService.Program> CompetitionFactory { get; private set; } =
        null!;

    public async Task InitializeAsync()
    {
        previousConnectionString =
            Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        previousCompetitionConnectionString =
            Environment.GetEnvironmentVariable(CompetitionConnectionStringEnvironmentVariable);
        previousIssuer = Environment.GetEnvironmentVariable(IssuerEnvironmentVariable);

        Environment.SetEnvironmentVariable(IssuerEnvironmentVariable, "https://localhost:5001/");

        await dbContainer.StartAsync();
        await competitionDbContainer.StartAsync();

        Environment.SetEnvironmentVariable(
            ConnectionStringEnvironmentVariable,
            dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable(
            CompetitionConnectionStringEnvironmentVariable,
            competitionDbContainer.GetConnectionString());

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });

        CompetitionFactory = new WebApplicationFactory<BenchCode.CompetitionService.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
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
            if (CompetitionFactory is not null)
                await CompetitionFactory.DisposeAsync();
        }
        finally
        {
            try
            {
                if (Factory is not null)
                    await Factory.DisposeAsync();
            }
            finally
            {
                try
                {
                    await competitionDbContainer.DisposeAsync();
                }
                finally
                {
                    try
                    {
                        await dbContainer.DisposeAsync();
                    }
                    finally
                    {
                        Environment.SetEnvironmentVariable(
                            ConnectionStringEnvironmentVariable,
                            previousConnectionString);
                        Environment.SetEnvironmentVariable(
                            CompetitionConnectionStringEnvironmentVariable,
                            previousCompetitionConnectionString);
                        Environment.SetEnvironmentVariable(IssuerEnvironmentVariable, previousIssuer);
                    }
                }
            }
        }
    }
}