extern alias CompetitionService;
using BenchCode.Shared;
using Competition = CompetitionService::BenchCode.CompetitionService.Entities.Competition;
using CompetitionStatus = CompetitionService::BenchCode.CompetitionService.Entities.CompetitionStatus;
using ContestPermission = CompetitionService::BenchCode.CompetitionService.Entities.ContestPermission;
using CompetitionTask = CompetitionService::BenchCode.CompetitionService.Entities.Task;
using CompetitionTest = CompetitionService::BenchCode.CompetitionService.Entities.Test;
using Participation = CompetitionService::BenchCode.CompetitionService.Entities.Participation;
using CompetitionVersion = CompetitionService::BenchCode.CompetitionService.Entities.CompetitionVersion;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using CompetitionDbContext = CompetitionService::BenchCode.CompetitionService.CompetitionDbContext;

namespace BenchCode.Test.IntegrationTests;

[Collection("AuthTests")]
public sealed class CompetitionTestsReadIntegrationTests(Fixture fixture)
{
    [Fact]
    public async Task TestReads_RespectPublicVisibilityAndCompetitionAccess()
    {
        var competitionId = Competition.GenerateId();
        var version = new CompetitionVersion
        {
            CompetitionId = competitionId,
            VersionNumber = 1,
            Name = "Test competition"
        };
        var task = new CompetitionTask
        {
            CompetitionVersionId = version.Id,
            OrderNo = 1,
            Name = "Task"
        };
        var publicTest = new CompetitionTest
        {
            TaskId = task.Id,
            OrderNo = 1,
            IsPublic = true,
            Input = "public input",
            Expected = "public output"
        };
        var privateTest = new CompetitionTest
        {
            TaskId = task.Id,
            OrderNo = 2,
            IsPublic = false,
            Input = "private input",
            Expected = "private output"
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
            db.Tests.AddRange(publicTest, privateTest);
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
            $"/internal/tasks/{task.Id}/tests");
        Assert.Equal(HttpStatusCode.OK, participantListResponse.StatusCode);
        using var participantListBody = JsonDocument.Parse(
            await participantListResponse.Content.ReadAsStringAsync());
        Assert.Single(participantListBody.RootElement.EnumerateArray());
        Assert.Equal(
            publicTest.Id.ToString(),
            participantListBody.RootElement[0].GetProperty("id").GetString());

        using var participantPrivateTest = await participant.GetAsync(
            $"/internal/tests/{privateTest.Id}");
        Assert.Equal(HttpStatusCode.NotFound, participantPrivateTest.StatusCode);

        using var owner = CreateClient("owner");
        using var ownerListResponse = await owner.GetAsync($"/internal/tasks/{task.Id}/tests");
        Assert.Equal(HttpStatusCode.OK, ownerListResponse.StatusCode);
        using var ownerListBody = JsonDocument.Parse(await ownerListResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, ownerListBody.RootElement.GetArrayLength());

        using var ownerPrivateTest = await owner.GetAsync($"/internal/tests/{privateTest.Id}");
        Assert.Equal(HttpStatusCode.OK, ownerPrivateTest.StatusCode);
        using var ownerPrivateTestBody = JsonDocument.Parse(
            await ownerPrivateTest.Content.ReadAsStringAsync());
        Assert.Equal(
            "private output",
            ownerPrivateTestBody.RootElement.GetProperty("expected").GetString());

        using var outsider = CreateClient("outsider");
        using var outsiderResponse = await outsider.GetAsync($"/internal/tests/{publicTest.Id}");
        Assert.Equal(HttpStatusCode.NotFound, outsiderResponse.StatusCode);
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
