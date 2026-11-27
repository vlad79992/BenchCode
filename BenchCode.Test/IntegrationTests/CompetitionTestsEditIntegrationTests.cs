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
public sealed class CompetitionTestsEditIntegrationTests(Fixture fixture)
{
    [Fact]
    public async Task EditAndDeleteTest_CreateNewVersionsAndPreserveOldRows()
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
        var firstTest = new CompetitionTest
        {
            TaskId = task.Id,
            OrderNo = 1,
            Input = "first input",
            Expected = "first expected"
        };
        var secondTest = new CompetitionTest
        {
            TaskId = task.Id,
            OrderNo = 2,
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
            db.Tasks.Add(task);
            db.Tests.AddRange(firstTest, secondTest);
            await db.SaveChangesAsync();
        }

        using var owner = CreateClient("owner");
        using var editResponse = await owner.PatchAsJsonAsync(
            $"/internal/tests/{firstTest.Id}",
            new { orderNo = 3, isPublic = true, input = "updated input", expected = "updated expected" });
        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);
        using var editBody = JsonDocument.Parse(await editResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, editBody.RootElement.GetProperty("version").GetInt32());

        Guid editedTestId;
        Guid copiedTaskId;
        Guid copiedSecondTestId;
        await using (var scope = fixture.CompetitionFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
            var editedVersion = await db.CompetitionVersions.AsNoTracking()
                .AsSplitQuery()
                .Include(item => item.Tasks)
                .ThenInclude(item => item.Tests)
                .SingleAsync(item => item.CompetitionId == competitionId && item.VersionNumber == 2);
            var copiedTask = Assert.Single(editedVersion.Tasks);
            var editedTest = copiedTask.Tests.Single(test => test.Expected == "updated expected");
            var copiedSecondTest = copiedTask.Tests.Single(test => test.Expected == "second expected");

            editedTestId = editedTest.Id;
            copiedTaskId = copiedTask.Id;
            copiedSecondTestId = copiedSecondTest.Id;

            Assert.NotEqual(task.Id, copiedTask.Id);
            Assert.NotEqual(firstTest.Id, editedTest.Id);
            Assert.NotEqual(secondTest.Id, copiedSecondTest.Id);
            Assert.Equal(3, editedTest.OrderNo);
            Assert.True(editedTest.IsPublic);
            Assert.Equal("updated input", editedTest.Input);
            Assert.Equal(copiedTask.Id, editedTest.TaskId);
        }

        using var deleteResponse = await owner.DeleteAsync($"/internal/tests/{editedTestId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        using var deleteBody = JsonDocument.Parse(await deleteResponse.Content.ReadAsStringAsync());
        Assert.Equal(3, deleteBody.RootElement.GetProperty("version").GetInt32());

        using var staleTestResponse = await owner.DeleteAsync($"/internal/tests/{editedTestId}");
        Assert.Equal(HttpStatusCode.NotFound, staleTestResponse.StatusCode);

        await using var verifyScope = fixture.CompetitionFactory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        var finalVersion = await verifyDb.CompetitionVersions.AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Tasks)
            .ThenInclude(item => item.Tests)
            .SingleAsync(item => item.CompetitionId == competitionId && item.VersionNumber == 3);

        var finalTask = Assert.Single(finalVersion.Tasks);
        var remainingTest = Assert.Single(finalTask.Tests);
        Assert.NotEqual(copiedTaskId, finalTask.Id);
        Assert.NotEqual(copiedSecondTestId, remainingTest.Id);
        Assert.Equal("second expected", remainingTest.Expected);
        Assert.True(await verifyDb.Tests.AnyAsync(test => test.Id == firstTest.Id));
        Assert.True(await verifyDb.Tests.AnyAsync(test => test.Id == secondTest.Id));
        Assert.True(await verifyDb.Tests.AnyAsync(test => test.Id == editedTestId));
    }

    [Fact]
    public async Task EditTest_RejectsNonOwnerAndTestsFromOldVersions()
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
        var test = new CompetitionTest
        {
            TaskId = task.Id,
            OrderNo = 1,
            Input = "input",
            Expected = "expected"
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
            db.Tests.Add(test);
            await db.SaveChangesAsync();
        }

        using var outsider = CreateClient("outsider");
        using var forbiddenResponse = await outsider.PatchAsJsonAsync(
            $"/internal/tests/{test.Id}",
            new { expected = "changed" });
        Assert.Equal(HttpStatusCode.NotFound, forbiddenResponse.StatusCode);

        using var owner = CreateClient("owner");
        using var successfulResponse = await owner.PatchAsJsonAsync(
            $"/internal/tests/{test.Id}",
            new { expected = "changed" });
        Assert.Equal(HttpStatusCode.OK, successfulResponse.StatusCode);

        using var staleResponse = await owner.PatchAsJsonAsync(
            $"/internal/tests/{test.Id}",
            new { expected = "stale change" });
        Assert.Equal(HttpStatusCode.NotFound, staleResponse.StatusCode);
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
