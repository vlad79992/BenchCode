extern alias CompetitionService;
using BenchCode.Shared;
using Competition = CompetitionService::BenchCode.CompetitionService.Entities.Competition;
using CompetitionStatus = CompetitionService::BenchCode.CompetitionService.Entities.CompetitionStatus;
using CompetitionTask = CompetitionService::BenchCode.CompetitionService.Entities.Task;
using CompetitionTest = CompetitionService::BenchCode.CompetitionService.Entities.Test;
using CompetitionVersion = CompetitionService::BenchCode.CompetitionService.Entities.CompetitionVersion;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CompetitionDbContext = CompetitionService::BenchCode.CompetitionService.CompetitionDbContext;

namespace BenchCode.Test.IntegrationTests;

[Collection("AuthTests")]
public sealed class CompetitionTestsCreateIntegrationTests(Fixture fixture)
{
    [Fact]
    public async Task CreateTest_AddsTestToNewVersionAndPreservesPreviousVersion()
    {
        var competitionId = Competition.GenerateId();
        var version = new CompetitionVersion
        {
            CompetitionId = competitionId,
            VersionNumber = 1,
            Name = "Competition"
        };
        var task = new CompetitionTask
        {
            CompetitionVersionId = version.Id,
            OrderNo = 1,
            Name = "Task"
        };
        var existingTest = new CompetitionTest
        {
            TaskId = task.Id,
            OrderNo = 1,
            IsPublic = false,
            Input = "existing input",
            Expected = "existing expected"
        };

        await using (var scope = fixture.CompetitionFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
            db.Competitions.Add(new Competition
            {
                Id = competitionId,
                CreatorId = "owner",
                Status = CompetitionStatus.Published,
                Version = 1
            });
            db.CompetitionVersions.Add(version);
            db.Tasks.Add(task);
            db.Tests.Add(existingTest);
            await db.SaveChangesAsync();
        }

        using var owner = CreateClient("owner");
        using var createResponse = await owner.PostAsJsonAsync(
            $"/internal/tasks/{task.Id}/tests",
            new { orderNo = 2, input = "", expected = "new expected", isPublic = true });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createBody = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, createBody.RootElement.GetProperty("version").GetInt32());
        Assert.Equal("", createBody.RootElement.GetProperty("input").GetString());
        Assert.True(createBody.RootElement.GetProperty("isPublic").GetBoolean());

        var copiedTaskId = createBody.RootElement.GetProperty("taskId").GetGuid();
        var createdTestId = createBody.RootElement.GetProperty("id").GetGuid();
        Assert.NotEqual(task.Id, copiedTaskId);

        await using (var scope = fixture.CompetitionFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
            var previousVersion = await db.CompetitionVersions.AsNoTracking()
                .AsSplitQuery()
                .Include(item => item.Tasks)
                .ThenInclude(item => item.Tests)
                .SingleAsync(item => item.CompetitionId == competitionId && item.VersionNumber == 1);
            var newVersion = await db.CompetitionVersions.AsNoTracking()
                .AsSplitQuery()
                .Include(item => item.Tasks)
                .ThenInclude(item => item.Tests)
                .SingleAsync(item => item.CompetitionId == competitionId && item.VersionNumber == 2);

            Assert.Equal(existingTest.Id, previousVersion.Tasks.Single().Tests.Single().Id);
            var currentTask = Assert.Single(newVersion.Tasks);
            Assert.NotEqual(task.Id, currentTask.Id);
            Assert.Equal(2, currentTask.Tests.Count);
            Assert.Contains(currentTask.Tests, test => test.Id == createdTestId &&
                test.Input == "" && test.Expected == "new expected" && test.IsPublic);
            Assert.Contains(currentTask.Tests, test => test.OrderNo == 1 &&
                test.Expected == "existing expected" && test.Id != existingTest.Id);
        }

        using var duplicateResponse = await owner.PostAsJsonAsync(
            $"/internal/tasks/{copiedTaskId}/tests",
            new { orderNo = 2, input = "another", expected = "duplicate" });
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        using var staleTaskResponse = await owner.PostAsJsonAsync(
            $"/internal/tasks/{task.Id}/tests",
            new { orderNo = 3, input = "stale", expected = "stale" });
        Assert.Equal(HttpStatusCode.NotFound, staleTaskResponse.StatusCode);
    }

    [Fact]
    public async Task CreateTest_RequiresCompetitionOwner()
    {
        var competitionId = Competition.GenerateId();
        var version = new CompetitionVersion
        {
            CompetitionId = competitionId,
            VersionNumber = 1,
            Name = "Competition"
        };
        var task = new CompetitionTask
        {
            CompetitionVersionId = version.Id,
            OrderNo = 1,
            Name = "Task"
        };

        await using (var scope = fixture.CompetitionFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
            db.Competitions.Add(new Competition
            {
                Id = competitionId,
                CreatorId = "owner",
                Status = CompetitionStatus.Published,
                Version = 1
            });
            db.CompetitionVersions.Add(version);
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
        }

        using var outsider = CreateClient("outsider");
        using var response = await outsider.PostAsJsonAsync(
            $"/internal/tasks/{task.Id}/tests",
            new { orderNo = 1, input = "input", expected = "expected" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private HttpClient CreateClient(string userId)
    {
        var client = fixture.CompetitionFactory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
        client.DefaultRequestHeaders.Add(InternalRequestHeaders.UserId, userId);
        return client;
    }
}
