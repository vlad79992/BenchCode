using BenchCode.CompetitionService;
using CompetitionEntity = BenchCode.CompetitionService.Entities.Competition;
using CompetitionStatus = BenchCode.CompetitionService.Entities.CompetitionStatus;
using ContestPermission = BenchCode.CompetitionService.Entities.ContestPermission;
using CompetitionTask = BenchCode.CompetitionService.Entities.Task;
using CompetitionTest = BenchCode.CompetitionService.Entities.Test;
using Participation = BenchCode.CompetitionService.Entities.Participation;
using CompetitionVersion = BenchCode.CompetitionService.Entities.CompetitionVersion;
using BenchCode.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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

        using var response = await client.PostAsJsonAsync("/internal/competitions", CreateRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCompetition_WithAuthentication_ReturnsCreatedCompetition()
    {
        using var client = CreateClient("test-owner");

        using var response = await client.PostAsJsonAsync("/internal/competitions", CreateRequest());

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
        using var created = await ownerClient.PostAsJsonAsync("/internal/competitions", CreateRequest());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var response = await ownerClient.GetAsync("/internal/me/competitions?scope=owned");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.NotEmpty(body.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task CreateCompetition_CreatesInitialVersionAndOwnerParticipation()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add(InternalRequestHeaders.UserId, "new-owner");

        using var response = await client.PostAsJsonAsync("/internal/competitions", new
        {
            name = "Initial competition",
            description = "Created in the first version.",
            allowViewOthersCode = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var id = body.RootElement.GetProperty("id").GetString();
        Assert.NotNull(id);
        Assert.Equal(1, body.RootElement.GetProperty("version").GetInt32());

        await using var scope = fixture.CompetitionFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        var competition = await db.Competitions.SingleAsync(item => item.Id == id);
        var version = await db.CompetitionVersions.SingleAsync(item => item.CompetitionId == id);
        var owner = await db.Participations.SingleAsync(item => item.CompetitionId == id);

        Assert.Equal("new-owner", competition.CreatorId);
        Assert.Equal(CompetitionStatus.Draft, competition.Status);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal("Initial competition", version.Name);
        Assert.Equal(ContestPermission.Owner, owner.Permission);
        Assert.Equal("new-owner", owner.UserId);

        using var publishResponse = await client.PatchAsJsonAsync(
            $"/internal/competitions/{id}/status",
            new { status = (int)CompetitionStatus.Published });
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        using var publishedBody = JsonDocument.Parse(await publishResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, publishedBody.RootElement.GetProperty("version").GetInt32());

        var publishedCompetition = await db.Competitions.AsNoTracking()
            .SingleAsync(item => item.Id == id);
        Assert.Equal(CompetitionStatus.Published, publishedCompetition.Status);
    }

    [Fact]
    public async Task EditCompetition_CopiesVersionAndStatusChangesDoNotIncrementIt()
    {
        var id = CompetitionEntity.GenerateId();
        await using var scope = fixture.CompetitionFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        db.Competitions.Add(new CompetitionEntity
        {
            Id = id,
            CreatorId = "editor",
            Status = CompetitionStatus.Published,
            Version = 1
        });
        var version = new CompetitionVersion
        {
            CompetitionId = id,
            VersionNumber = 1,
            Name = "Original"
        };
        db.CompetitionVersions.Add(version);
        var oldTask = new CompetitionTask
        {
            CompetitionVersionId = version.Id,
            OrderNo = 1,
            Name = "Original task"
        };
        var oldTest = new CompetitionTest
        {
            TaskId = oldTask.Id,
            OrderNo = 1,
            IsPublic = false,
            Input = "input",
            Expected = "expected"
        };
        db.Tasks.Add(oldTask);
        db.Tests.Add(oldTest);
        await db.SaveChangesAsync();

        using var client = CreateClient();
        client.DefaultRequestHeaders.Add(InternalRequestHeaders.UserId, "editor");
        using var editResponse = await client.PatchAsJsonAsync($"/internal/competitions/{id}", new
        {
            name = "Updated",
            allowViewOthersCode = true
        });

        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);
        using var editBody = JsonDocument.Parse(await editResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, editBody.RootElement.GetProperty("version").GetInt32());

        using var archiveResponse = await client.PatchAsJsonAsync(
            $"/internal/competitions/{id}/status",
            new { status = (int)CompetitionStatus.Archived });
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        using var archiveBody = JsonDocument.Parse(await archiveResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, archiveBody.RootElement.GetProperty("version").GetInt32());
        Assert.Equal((int)CompetitionStatus.Archived, archiveBody.RootElement.GetProperty("status").GetInt32());

        using var restoreResponse = await client.PatchAsJsonAsync(
            $"/internal/competitions/{id}/status",
            new { status = (int)CompetitionStatus.Published });
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
        using var restoreBody = JsonDocument.Parse(await restoreResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, restoreBody.RootElement.GetProperty("version").GetInt32());

        await using var verificationScope = fixture.CompetitionFactory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        var competition = await verificationDb.Competitions.AsNoTracking()
            .SingleAsync(item => item.Id == id);
        var versions = await verificationDb.CompetitionVersions.AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Tasks)
            .ThenInclude(task => task.Tests)
            .Where(item => item.CompetitionId == id)
            .OrderBy(item => item.VersionNumber)
            .ToListAsync();

        Assert.Equal(CompetitionStatus.Published, competition.Status);
        Assert.Equal(2, competition.Version);
        Assert.Equal("Original", versions[0].Name);
        Assert.Equal("Updated", versions[1].Name);
        Assert.NotEqual(oldTask.Id, versions[1].Tasks.Single().Id);
        Assert.NotEqual(oldTest.Id, versions[1].Tasks.Single().Tests.Single().Id);
        Assert.Equal(versions[1].Tasks.Single().Id, versions[1].Tasks.Single().Tests.Single().TaskId);
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
            client.DefaultRequestHeaders.Add(InternalRequestHeaders.UserId, userId);
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