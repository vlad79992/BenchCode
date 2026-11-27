extern alias Gateway;
using System.Net;
using System.Net.Http.Headers;
using BenchCode.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BenchCode.Test.IntegrationTests;

public sealed class GatewayRateLimiterTests
{
    [Fact]
    public async Task ApiProxy_RewritesPathAndForwardsOnlyAuthenticatedUserId()
    {
        await using var upstream = await TestUpstream.StartAsync();
        await using var factory = CreateFactory(upstream.Address);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", "trusted-user");
        client.DefaultRequestHeaders.Add(InternalRequestHeaders.UserId, "spoofed-user");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        using var response = await client.GetAsync("/api/me/competitions?scope=owned");
        var forwarded = await upstream.ForwardedRequest;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/internal/me/competitions", forwarded.Path);
        Assert.Equal("trusted-user", forwarded.UserId);
        Assert.Null(forwarded.Authorization);
    }

    [Fact]
    [Trait("Category", "RateLimitIntegration")]
    public async Task GlobalLimit_IsSharedForAllPaths()
    {
        await using var upstream = await TestUpstream.StartAsync();
        await using var factory = CreateFactory(upstream.Address);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", "rate-limit-test");

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
        await using var upstream = await TestUpstream.StartAsync();
        await using var factory = CreateFactory(upstream.Address);
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

    private static WebApplicationFactory<Gateway::Program> CreateFactory(string upstreamAddress) =>
        new WebApplicationFactory<Gateway::Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("OpenIddict:Issuer", "https://localhost:5001/");
                builder.UseSetting("ReverseProxy:AuthAddress", upstreamAddress);
                builder.UseSetting("ReverseProxy:CompetitionAddress", upstreamAddress);
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

    private sealed record ForwardedRequest(string Path, string? UserId, string? Authorization);

    private sealed class TestUpstream(
        WebApplication app,
        string address,
        Task<ForwardedRequest> forwardedRequest) : IAsyncDisposable
    {
        public string Address { get; } = address;
        public Task<ForwardedRequest> ForwardedRequest { get; } = forwardedRequest;

        public ValueTask DisposeAsync() => app.DisposeAsync();

        public static async Task<TestUpstream> StartAsync()
        {
            var receivedRequest = new TaskCompletionSource<ForwardedRequest>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var app = builder.Build();
            app.Run(async context =>
            {
                receivedRequest.TrySetResult(new ForwardedRequest(
                    context.Request.Path.Value ?? string.Empty,
                    context.Request.Headers[InternalRequestHeaders.UserId].FirstOrDefault(),
                    context.Request.Headers["Authorization"].FirstOrDefault()));
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsync("ok");
            });
            await app.StartAsync();

            var server = app.Services.GetRequiredService<IServer>();
            var address = server.Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            return new TestUpstream(app, address, receivedRequest.Task);
        }
    }
}
