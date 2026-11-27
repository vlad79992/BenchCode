extern alias CompetitionService;
using BenchCode.Shared;
using Competition = CompetitionService::BenchCode.CompetitionService.Entities.Competition;
using CompetitionStatus = CompetitionService::BenchCode.CompetitionService.Entities.CompetitionStatus;
using ContestPermission = CompetitionService::BenchCode.CompetitionService.Entities.ContestPermission;
using CompetitionTask = CompetitionService::BenchCode.CompetitionService.Entities.Task;
using Participation = CompetitionService::BenchCode.CompetitionService.Entities.Participation;
using CompetitionVersion = CompetitionService::BenchCode.CompetitionService.Entities.CompetitionVersion;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using CompetitionDbContext = CompetitionService::BenchCode.CompetitionService.CompetitionDbContext;

namespace BenchCode.Test.IntegrationTests;

[Collection("AuthTests")]
public sealed class CompetitionTasksReadIntegrationTests(Fixture fixture)
{
    [Fact]
    public async Task TaskReads_UseCurrentVersionAndRespectArchivedVisibility()
    {
        var competitionId = Competition.GenerateId();
        var previousVersion = new CompetitionVersion
        {
            CompetitionId = competitionId,
            VersionNumber = 1,
            Name = "Previous"
        };
        var currentVersion = new CompetitionVersion
        {
            CompetitionId = competitionId,
            VersionNumber = 2,
            Name = "Current"
        };
        var previousTask = new CompetitionTask
        {
            CompetitionVersionId = previousVersion.Id,
            OrderNo = 1,
            Name = "Old task"
        };
        var visibleTask = new CompetitionTask
        {
            CompetitionVersionId = currentVersion.Id,
            OrderNo = 1,
            Name = "Visible task"
        };
        var archivedTask = new CompetitionTask
        {
            CompetitionVersionId = currentVersion.Id,
            OrderNo = 2,
            Name = "Archived task",
            IsArchived = true
        };

        await using (var scope = fixture.CompetitionFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
            db.Competitions.Add(new Competition
            {
                Id = competitionId,
                CreatorId = "owner",
                Status = CompetitionStatus.Archived,
                Version = 2
            });
            db.CompetitionVersions.AddRange(previousVersion, currentVersion);
            db.Tasks.AddRange(previousTask, visibleTask, archivedTask);
            db.Participations.AddRange(
                new Participation
                {
                    CompetitionId = competitionId,
                    UserId = "owner",
                    Permission = ContestPermission.Owner
                },
                new Participation
                {
                    CompetitionId = competitionId,
                    UserId = "participant"
                });
            await db.SaveChangesAsync();
        }

        using var participant = CreateClient("participant");
        using var participantListResponse = await participant.GetAsync(
            $"/internal/competitions/{competitionId}/tasks");
        Assert.Equal(HttpStatusCode.OK, participantListResponse.StatusCode);
        using var participantListBody = JsonDocument.Parse(
            await participantListResponse.Content.ReadAsStringAsync());
        Assert.Single(participantListBody.RootElement.EnumerateArray());
        Assert.Equal(
            "Visible task",
            participantListBody.RootElement[0].GetProperty("name").GetString());

        using var participantArchivedTask = await participant.GetAsync(
            $"/internal/tasks/{archivedTask.Id}");
        Assert.Equal(HttpStatusCode.NotFound, participantArchivedTask.StatusCode);

        using var owner = CreateClient("owner");
        using var ownerListResponse = await owner.GetAsync(
            $"/internal/competitions/{competitionId}/tasks");
        Assert.Equal(HttpStatusCode.OK, ownerListResponse.StatusCode);
        using var ownerListBody = JsonDocument.Parse(await ownerListResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, ownerListBody.RootElement.GetArrayLength());

        using var ownerArchivedTask = await owner.GetAsync($"/internal/tasks/{archivedTask.Id}");
        Assert.Equal(HttpStatusCode.OK, ownerArchivedTask.StatusCode);
        using var ownerArchivedTaskBody = JsonDocument.Parse(await ownerArchivedTask.Content.ReadAsStringAsync());
        Assert.Equal("Archived task", ownerArchivedTaskBody.RootElement.GetProperty("name").GetString());

        using var participantVisibleTask = await participant.GetAsync($"/internal/tasks/{visibleTask.Id}");
        Assert.Equal(HttpStatusCode.OK, participantVisibleTask.StatusCode);
        using var participantVisibleTaskBody = JsonDocument.Parse(
            await participantVisibleTask.Content.ReadAsStringAsync());
        Assert.Equal(2, participantVisibleTaskBody.RootElement.GetProperty("version").GetInt32());
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
