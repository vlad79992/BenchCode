extern alias Gateway;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BenchCode.Test.IntegrationTests;

public sealed class GatewayRateLimiterTests
{
    [Fact]
    [Trait("Category", "RateLimitIntegration")]
    public async Task GlobalLimit_IsSharedForAllPaths()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        for (var i = 0; i < 100; i++)
        {
            // Проверка, что глобальный лимит применяется ко всем путям, а не к каждому пути отдельно
            var path = i % 2 == 0 ? "/api/first" : "/api/second";
            using var response = await client.GetAsync(path);

            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        using var rejected = await client.GetAsync("/api/first");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    [Fact]
    [Trait("Category", "RateLimitIntegration")]
    public async Task AuthPolicy_AllowsOnlyTenRequests()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        for (var i = 0; i < 10; i++)
        {
            // Ограничение применяется ко всем путям /auth/...
            // Поэтому проверяется не реальный путь
            using var response = await client.GetAsync("/auth/rate-limit-probe");

            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        using var rejected = await client.GetAsync("/auth/rate-limit-probe");
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    private static WebApplicationFactory<Gateway::Program> CreateFactory() =>
        new WebApplicationFactory<Gateway::Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
}
