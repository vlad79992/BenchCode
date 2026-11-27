using BenchCode.CompetitionService;
using CompetitionEntity = BenchCode.CompetitionService.Entities.Competition;
using CompetitionStatus = BenchCode.CompetitionService.Entities.CompetitionStatus;
using CompetitionTask = BenchCode.CompetitionService.Entities.Task;
using CompetitionTest = BenchCode.CompetitionService.Entities.Test;
using CompetitionVersion = BenchCode.CompetitionService.Entities.CompetitionVersion;
using BenchCode.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BenchCode.Test.IntegrationTests;

[Collection("AuthTests")]
public sealed class CompetitionIntegrationTests(Fixture fixture)
{
    private readonly Fixture fixture = fixture;

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

    [Fact]
    public async Task GetCompetitionById_ReturnsCurrentArchivedVersion()
    {
        var id = CompetitionEntity.GenerateId();
        await using var scope = fixture.CompetitionFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        db.Competitions.Add(new CompetitionEntity
        {
            Id = id,
            CreatorId = "owner",
            Status = CompetitionStatus.Archived,
            Version = 2
        });
        var previousVersion = new CompetitionVersion
        {
            CompetitionId = id,
            VersionNumber = 1,
            Name = "Previous version"
        };
        var currentVersion = new CompetitionVersion
        {
            CompetitionId = id,
            VersionNumber = 2,
            Name = "Current version",
            Description = "Archived but readable"
        };
        db.CompetitionVersions.AddRange(previousVersion, currentVersion);

        var task = new CompetitionTask
        {
            CompetitionVersionId = currentVersion.Id,
            OrderNo = 1,
            Name = "Version-specific task"
        };
        db.Tasks.Add(task);
        db.Tests.Add(new CompetitionTest
        {
            TaskId = task.Id,
            OrderNo = 1,
            IsPublic = false,
            Input = "private input",
            Expected = "private expected"
        });
        await db.SaveChangesAsync();

        using var client = CreateClient();
        client.DefaultRequestHeaders.Add(InternalRequestHeaders.UserId, "reader");
        using var response = await client.GetAsync($"/internal/competitions/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(id, body.RootElement.GetProperty("id").GetString());
        Assert.Equal("Current version", body.RootElement.GetProperty("name").GetString());
        Assert.Equal(2, body.RootElement.GetProperty("version").GetInt32());

        using var hiddenHistoryResponse = await client.GetAsync($"/internal/competitions/{id}/versions");
        Assert.Equal(HttpStatusCode.NotFound, hiddenHistoryResponse.StatusCode);

        using var ownerClient = CreateClient();
        ownerClient.DefaultRequestHeaders.Add(InternalRequestHeaders.UserId, "owner");
        using var historyResponse = await ownerClient.GetAsync($"/internal/competitions/{id}/versions");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        using var historyBody = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, historyBody.RootElement.GetArrayLength());
        Assert.Equal(2, historyBody.RootElement[0].GetProperty("version").GetInt32());

        using var versionResponse = await ownerClient.GetAsync($"/internal/competitions/{id}/versions/2");
        Assert.Equal(HttpStatusCode.OK, versionResponse.StatusCode);
        using var versionBody = JsonDocument.Parse(await versionResponse.Content.ReadAsStringAsync());
        var taskBody = versionBody.RootElement.GetProperty("tasks")[0];
        Assert.Equal("Version-specific task", taskBody.GetProperty("name").GetString());
        var testBody = taskBody.GetProperty("tests")[0];
        Assert.Equal("private input", testBody.GetProperty("input").GetString());
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