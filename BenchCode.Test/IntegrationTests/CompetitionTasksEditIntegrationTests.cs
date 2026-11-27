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
public sealed class CompetitionTasksEditIntegrationTests(Fixture fixture)
{
    [Fact]
    public async Task EditAndDeleteTask_CreateNewVersionsAndPreserveOldRows()
    {
        var competitionId = Competition.GenerateId();
        var version = new CompetitionVersion
        {
            CompetitionId = competitionId,
            VersionNumber = 1,
            Name = "Competition"
        };
        var firstTask = new CompetitionTask
        {
            CompetitionVersionId = version.Id,
            OrderNo = 1,
            Name = "First task"
        };
        var secondTask = new CompetitionTask
        {
            CompetitionVersionId = version.Id,
            OrderNo = 2,
            Name = "Second task"
        };
        var firstTest = new CompetitionTest
        {
            TaskId = firstTask.Id,
            OrderNo = 1,
            IsPublic = false,
            Input = "first input",
            Expected = "first expected"
        };
        var secondTest = new CompetitionTest
        {
            TaskId = secondTask.Id,
            OrderNo = 1,
            IsPublic = false,
            Input = "second input",
            Expected = "second expected"
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
            db.Tasks.AddRange(firstTask, secondTask);
            db.Tests.AddRange(firstTest, secondTest);
            await db.SaveChangesAsync();
        }

        using var owner = CreateClient("owner");
        using var editResponse = await owner.PatchAsJsonAsync(
            $"/internal/tasks/{firstTask.Id}",
            new { name = "Updated first task", isArchived = true });
        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);
        using var editBody = JsonDocument.Parse(await editResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, editBody.RootElement.GetProperty("version").GetInt32());

        Guid copiedFirstTaskId;
        Guid copiedFirstTestId;
        Guid copiedSecondTaskId;
        await using (var scope = fixture.CompetitionFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
            var newVersion = await db.CompetitionVersions.AsNoTracking()
                .AsSplitQuery()
                .Include(item => item.Tasks)
                .ThenInclude(task => task.Tests)
                .SingleAsync(item => item.CompetitionId == competitionId && item.VersionNumber == 2);
            var copiedFirstTask = newVersion.Tasks.Single(task => task.Name == "Updated first task");
            var copiedSecondTask = newVersion.Tasks.Single(task => task.Name == "Second task");
            copiedFirstTaskId = copiedFirstTask.Id;
            copiedFirstTestId = copiedFirstTask.Tests.Single().Id;
            copiedSecondTaskId = copiedSecondTask.Id;

            Assert.True(copiedFirstTask.IsArchived);
            Assert.NotEqual(firstTask.Id, copiedFirstTask.Id);
            Assert.NotEqual(secondTask.Id, copiedSecondTask.Id);
            Assert.NotEqual(firstTest.Id, copiedFirstTask.Tests.Single().Id);
            Assert.Equal(copiedFirstTask.Id, copiedFirstTask.Tests.Single().TaskId);
        }

        using var deleteResponse = await owner.DeleteAsync($"/internal/tasks/{copiedSecondTaskId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        using var deleteBody = JsonDocument.Parse(await deleteResponse.Content.ReadAsStringAsync());
        Assert.Equal(3, deleteBody.RootElement.GetProperty("version").GetInt32());

        await using var verifyScope = fixture.CompetitionFactory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        var finalVersion = await verifyDb.CompetitionVersions.AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Tasks)
            .ThenInclude(task => task.Tests)
            .SingleAsync(item => item.CompetitionId == competitionId && item.VersionNumber == 3);

        Assert.Single(finalVersion.Tasks);
        Assert.NotEqual(copiedFirstTaskId, finalVersion.Tasks.Single().Id);
        Assert.NotEqual(copiedFirstTestId, finalVersion.Tasks.Single().Tests.Single().Id);
        Assert.Equal(finalVersion.Tasks.Single().Id, finalVersion.Tasks.Single().Tests.Single().TaskId);
        Assert.Equal("Updated first task", finalVersion.Tasks.Single().Name);
        Assert.True(await verifyDb.Tasks.AnyAsync(task => task.Id == firstTask.Id));
        Assert.True(await verifyDb.Tasks.AnyAsync(task => task.Id == secondTask.Id));
        Assert.True(await verifyDb.Tests.AnyAsync(test => test.Id == firstTest.Id));
        Assert.True(await verifyDb.Tests.AnyAsync(test => test.Id == secondTest.Id));
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
