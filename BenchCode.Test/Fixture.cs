extern alias AuthService;
extern alias CompetitionService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using BenchCode.Test.IntegrationTests;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using CompetitionSubmissionRunner = CompetitionService::BenchCode.CompetitionService.Services.ISubmissionRunner;

namespace BenchCode.Test;

public sealed class Fixture : IAsyncLifetime
{
    private const string PostgresImage = "postgres:19.0";

    private readonly PostgreSqlContainer authDbContainer =
        new PostgreSqlBuilder(PostgresImage).Build();
    private readonly PostgreSqlContainer competitionDbContainer =
        new PostgreSqlBuilder(PostgresImage).Build();
    private readonly RabbitMqContainer rabbitMqContainer = new RabbitMqBuilder("rabbitmq:4.1-management").Build();


    public WebApplicationFactory<AuthService::Program> AuthServiceFactory { get; private set; } = null!;
    public WebApplicationFactory<CompetitionService::Program> CompetitionFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {

        await authDbContainer.StartAsync();
        await competitionDbContainer.StartAsync();
        await rabbitMqContainer.StartAsync();


        AuthServiceFactory = new WebApplicationFactory<AuthService::Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("ConnectionStrings:authdb", authDbContainer.GetConnectionString());
                builder.UseSetting("OpenIddict:Issuer", "https://localhost:5001/");
            });

        CompetitionFactory = new WebApplicationFactory<CompetitionService::Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting(
                    "ConnectionStrings:competitionsdb",
                    competitionDbContainer.GetConnectionString());
                builder.UseSetting("OpenIddict:Issuer", "https://localhost:5001/");
                builder.UseSetting("ConnectionStrings:rabbitmq", rabbitMqContainer.GetConnectionString());
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<CompetitionSubmissionRunner, FixtureSubmissionRunner>();
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
        await using var rabbitMq = rabbitMqContainer;
        await using var userFactory = AuthServiceFactory;
        await using var competitionFactory = CompetitionFactory;
    }
}