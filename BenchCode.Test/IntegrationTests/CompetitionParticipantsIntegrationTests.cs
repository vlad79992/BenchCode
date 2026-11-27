extern alias CompetitionService;
using BenchCode.Shared;
using Competition = CompetitionService::BenchCode.CompetitionService.Entities.Competition;
using CompetitionStatus = CompetitionService::BenchCode.CompetitionService.Entities.CompetitionStatus;
using ContestPermission = CompetitionService::BenchCode.CompetitionService.Entities.ContestPermission;
using Participation = CompetitionService::BenchCode.CompetitionService.Entities.Participation;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CompetitionDbContext = CompetitionService::BenchCode.CompetitionService.CompetitionDbContext;

namespace BenchCode.Test.IntegrationTests;

[Collection("AuthTests")]
public sealed class CompetitionParticipantsIntegrationTests(Fixture fixture)
{
    [Fact]
    public async Task ParticipantsList_HidesOtherHiddenUsersButShowsSelf()
    {
        var competitionId = Competition.GenerateId();
        await using (var scope = fixture.CompetitionFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
            db.Competitions.Add(new Competition
            {
                Id = competitionId,
                CreatorId = "owner",
                Status = CompetitionStatus.Published
            });
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
                    UserId = "hidden-one",
                    IsHidden = true
                },
                new Participation
                {
                    CompetitionId = competitionId,
                    UserId = "hidden-two",
                    IsHidden = true
                },
                new Participation
                {
                    CompetitionId = competitionId,
                    UserId = "visible-user"
                });
            await db.SaveChangesAsync();
        }

        using var owner = CreateClient("owner");
        using var ownerResponse = await owner.GetAsync($"/internal/competitions/{competitionId}/participants");
        Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);
        using var ownerBody = JsonDocument.Parse(await ownerResponse.Content.ReadAsStringAsync());
        Assert.Equal(4, ownerBody.RootElement.GetArrayLength());
        Assert.Contains(ownerBody.RootElement.EnumerateArray(), participant =>
            participant.GetProperty("userId").GetString() == "hidden-one" &&
            participant.GetProperty("isHidden").GetBoolean());

        using var visibleUser = CreateClient("visible-user");
        using var visibleResponse = await visibleUser.GetAsync($"/internal/competitions/{competitionId}/participants");
        using var visibleBody = JsonDocument.Parse(await visibleResponse.Content.ReadAsStringAsync());
        var visibleIds = GetUserIds(visibleBody.RootElement);
        Assert.Contains("owner", visibleIds);
        Assert.Contains("visible-user", visibleIds);
        Assert.DoesNotContain("hidden-one", visibleIds);
        Assert.DoesNotContain("hidden-two", visibleIds);

        using var hiddenUser = CreateClient("hidden-one");
        using var hiddenResponse = await hiddenUser.GetAsync($"/internal/competitions/{competitionId}/participants");
        using var hiddenBody = JsonDocument.Parse(await hiddenResponse.Content.ReadAsStringAsync());
        var hiddenIds = GetUserIds(hiddenBody.RootElement);
        Assert.Contains("owner", hiddenIds);
        Assert.Contains("visible-user", hiddenIds);
        Assert.Contains("hidden-one", hiddenIds);
        Assert.DoesNotContain("hidden-two", hiddenIds);

        using var setVisibilityResponse = await owner.PatchAsJsonAsync(
            $"/internal/competitions/{competitionId}/participants/hidden-two/visibility",
            new { isHidden = false });
        Assert.Equal(HttpStatusCode.OK, setVisibilityResponse.StatusCode);

        using var updatedVisibleResponse = await visibleUser.GetAsync(
            $"/internal/competitions/{competitionId}/participants");
        using var updatedVisibleBody = JsonDocument.Parse(await updatedVisibleResponse.Content.ReadAsStringAsync());
        var updatedVisibleIds = GetUserIds(updatedVisibleBody.RootElement);
        Assert.Contains("hidden-two", updatedVisibleIds);
        Assert.DoesNotContain("hidden-one", updatedVisibleIds);

        using var outsider = CreateClient("outsider");
        using var outsiderResponse = await outsider.GetAsync(
            $"/internal/competitions/{competitionId}/participants");
        Assert.Equal(HttpStatusCode.NotFound, outsiderResponse.StatusCode);
    }

    [Fact]
    public async Task JoinCompetition_CreatesParticipationAndRejectsDuplicate()
    {
        var competitionId = Competition.GenerateId();
        await using (var scope = fixture.CompetitionFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
            db.Competitions.Add(new Competition
            {
                Id = competitionId,
                CreatorId = "owner",
                Status = CompetitionStatus.Published
            });
            db.Participations.Add(new Participation
            {
                CompetitionId = competitionId,
                UserId = "owner",
                Permission = ContestPermission.Owner
            });
            await db.SaveChangesAsync();
        }

        using var client = CreateClient("new-participant");
        var path = $"/internal/competitions/{competitionId}/join";
        using var response = await client.PostAsJsonAsync(path, new { });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var duplicateResponse = await client.PostAsJsonAsync(path, new { });
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        await using var verificationScope = fixture.CompetitionFactory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        var participation = await verificationDb.Participations.SingleAsync(item =>
            item.CompetitionId == competitionId && item.UserId == "new-participant");
        Assert.Equal(ContestPermission.Participant, participation.Permission);
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

    private static string?[] GetUserIds(JsonElement participants) =>
        participants.EnumerateArray()
            .Select(participant => participant.GetProperty("userId").GetString())
            .ToArray();
}
