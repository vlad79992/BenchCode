using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace BenchCode.Test;

public sealed class AuthFixture : IAsyncLifetime
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__authdb";

    private readonly PostgreSqlContainer dbContainer =
        new PostgreSqlBuilder("postgres:16-alpine").Build();

    private string? previousConnectionString;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        previousConnectionString =
            Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);

        await dbContainer.StartAsync();

        Environment.SetEnvironmentVariable(
            ConnectionStringEnvironmentVariable,
            dbContainer.GetConnectionString());

        Factory = new WebApplicationFactory<Program>()
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
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (Factory is not null)
            {
                await Factory.DisposeAsync();
            }
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
            }
        }
    }
}