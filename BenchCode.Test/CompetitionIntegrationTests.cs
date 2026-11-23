using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BenchCode.Test;

[Collection("AuthTests")]
public sealed class CompetitionIntegrationTests
{
    private readonly AuthFixture fixture;

    public CompetitionIntegrationTests(AuthFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task CreateCompetition_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync("/api/competitions", CreateRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCompetition_WithAuthentication_ReturnsCreatedCompetition()
    {
        using var client = CreateClient("test-owner");

        using var response = await client.PostAsJsonAsync("/api/competitions", CreateRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Основы программирования", body.RootElement.GetProperty("name").GetString());
        Assert.False(body.RootElement.TryGetProperty("group", out _));
        Assert.Equal("test-owner", body.RootElement.GetProperty("creatorId").GetString());
    }

    [Fact]
    public async Task GetMyCompetitions_ReturnsOnlyOwnedOrJoinedCompetitions()
    {
        using var ownerClient = CreateClient("owner");
        using var created = await ownerClient.PostAsJsonAsync("/api/competitions", CreateRequest());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var response = await ownerClient.GetAsync("/api/me/competitions?scope=owned");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.NotEmpty(body.RootElement.EnumerateArray());
    }

    private HttpClient CreateClient(string? userId = null)
    {
        var client = fixture.CompetitionFactory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
        if (userId is not null)
            client.DefaultRequestHeaders.Add("X-Test-User-Id", userId);
        return client;
    }

    private static object CreateRequest() => new
    {
        name = "Основы программирования",
        description = "Практическое соревнование.",
        deadline = (DateTimeOffset?)null,
        weightTime = 0.5,
        weightMemory = 0.5,
        allowViewOthersCode = false,
        defaultTimeLimitMs = 1000,
        defaultMemoryLimitMb = 256
    };
}