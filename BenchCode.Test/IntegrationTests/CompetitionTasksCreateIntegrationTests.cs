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
public sealed class CompetitionTasksCreateIntegrationTests(Fixture fixture)
{
    [Fact]
    public async Task CreateTask_CopiesCurrentVersionAndAddsTask()
    {
        var competitionId = Competition.GenerateId();
        var currentVersion = new CompetitionVersion
        {
            CompetitionId = competitionId,
            VersionNumber = 1,
            Name = "Competition"
        };
        var existingTask = new CompetitionTask
        {
            CompetitionVersionId = currentVersion.Id,
            OrderNo = 1,
            Name = "Existing task"
        };
        var existingTest = new CompetitionTest
        {
            TaskId = existingTask.Id,
            OrderNo = 1,
            IsPublic = false,
            Input = "old input",
            Expected = "old expected"
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
            db.CompetitionVersions.Add(currentVersion);
            db.Tasks.Add(existingTask);
            db.Tests.Add(existingTest);
            await db.SaveChangesAsync();
        }

        using var owner = CreateClient("owner");
        var path = $"/internal/competitions/{competitionId}/tasks";
        using var response = await owner.PostAsJsonAsync(path, new
        {
            name = "New task",
            orderNo = 2,
            timeLimitMs = 2000,
            memoryLimitMb = 512
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var responseBody = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(2, responseBody.RootElement.GetProperty("version").GetInt32());
        var createdTaskId = responseBody.RootElement.GetProperty("id").GetGuid();

        using var duplicateOrderResponse = await owner.PostAsJsonAsync(path, new
        {
            name = "Duplicate order",
            orderNo = 2
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicateOrderResponse.StatusCode);

        await using var verificationScope = fixture.CompetitionFactory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        var versions = await verificationDb.CompetitionVersions.AsNoTracking()
            .AsSplitQuery()
            .Include(version => version.Tasks)
            .ThenInclude(task => task.Tests)
            .Where(version => version.CompetitionId == competitionId)
            .OrderBy(version => version.VersionNumber)
            .ToListAsync();

        Assert.Equal(2, versions.Count);
        Assert.Single(versions[0].Tasks);
        Assert.Equal(existingTask.Id, versions[0].Tasks.Single().Id);
        Assert.Equal(existingTest.Id, versions[0].Tasks.Single().Tests.Single().Id);

        Assert.Equal(2, versions[1].Tasks.Count);
        var copiedTask = versions[1].Tasks.Single(task => task.Name == "Existing task");
        Assert.NotEqual(existingTask.Id, copiedTask.Id);
        Assert.NotEqual(existingTest.Id, copiedTask.Tests.Single().Id);
        Assert.Equal(copiedTask.Id, copiedTask.Tests.Single().TaskId);
        Assert.Contains(versions[1].Tasks, task => task.Id == createdTaskId && task.Name == "New task");

        var competition = await verificationDb.Competitions.AsNoTracking()
            .SingleAsync(item => item.Id == competitionId);
        Assert.Equal(2, competition.Version);
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
