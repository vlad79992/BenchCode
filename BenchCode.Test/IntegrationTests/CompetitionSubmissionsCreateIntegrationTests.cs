extern alias CompetitionService;
using BenchCode.Shared;
using SharedVerdict = BenchCode.Shared.Entities.Verdict;
using Competition = CompetitionService::BenchCode.CompetitionService.Entities.Competition;
using CompetitionStatus = CompetitionService::BenchCode.CompetitionService.Entities.CompetitionStatus;
using CompetitionLanguage = CompetitionService::BenchCode.CompetitionService.Entities.Language;
using ContestPermission = CompetitionService::BenchCode.CompetitionService.Entities.ContestPermission;
using CompetitionTask = CompetitionService::BenchCode.CompetitionService.Entities.Task;
using CompetitionTest = CompetitionService::BenchCode.CompetitionService.Entities.Test;
using Submission = CompetitionService::BenchCode.CompetitionService.Entities.Submission;
using SubmissionStatus = CompetitionService::BenchCode.CompetitionService.Entities.SubmissionStatus;
using Participation = CompetitionService::BenchCode.CompetitionService.Entities.Participation;
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
public sealed class CompetitionSubmissionsCreateIntegrationTests(Fixture fixture)
{
    [Fact]
    public async Task PublicCheck_RunsOnlyPublicTestsAndDoesNotCreateSubmission()
    {
        var competitionId = Competition.GenerateId();
        var version = new CompetitionVersion
        {
            CompetitionId = competitionId,
            VersionNumber = 1,
            Name = "Archived practice",
            StartTime = DateTimeOffset.UtcNow.AddHours(-2),
            EndTime = DateTimeOffset.UtcNow.AddHours(-1)
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
            Expected = "public expected"
        };
        var hiddenTest = new CompetitionTest
        {
            TaskId = task.Id,
            OrderNo = 2,
            IsPublic = false,
            Input = "hidden input",
            Expected = "hidden expected"
        };

        await SeedTaskAsync(competitionId, version, task, [publicTest, hiddenTest],
            CompetitionStatus.Archived, "participant");

        using var participant = CreateClient("participant");
        using var response = await participant.PostAsJsonAsync(
            $"/internal/tasks/{task.Id}/submissions",
            new
            {
                mode = "PublicCheck",
                language = "Cpp",
                code = "int main() { return 0; }"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var runs = body.RootElement.GetProperty("runs").EnumerateArray().ToArray();
        Assert.Single(runs);
        Assert.Equal(publicTest.Id.ToString(), runs[0].GetProperty("testId").GetString());

        await using var scope = fixture.CompetitionFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        Assert.False(await db.Submissions.AnyAsync(submission => submission.TaskId == task.Id));
        Assert.False(await db.Runs.AnyAsync(run => run.TestId == publicTest.Id || run.TestId == hiddenTest.Id));
    }

    [Fact]
    public async Task FullSubmission_IsPersistedAndQueuedOnlyDuringPublishedWindow()
    {
        var competitionId = Competition.GenerateId();
        var version = new CompetitionVersion
        {
            CompetitionId = competitionId,
            VersionNumber = 1,
            Name = "Live competition",
            StartTime = DateTimeOffset.UtcNow.AddHours(-1),
            EndTime = DateTimeOffset.UtcNow.AddHours(1)
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
            IsPublic = false,
            Input = "private input",
            Expected = "private expected"
        };

        await SeedTaskAsync(competitionId, version, task, [test],
            CompetitionStatus.Published, "participant");

        using var participant = CreateClient("participant");
        using var response = await participant.PostAsJsonAsync(
            $"/internal/tasks/{task.Id}/submissions",
            new
            {
                mode = "FullSubmission",
                language = "Cpp",
                code = "int main() { return 0; }"
            });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var submissionId = body.RootElement.GetProperty("id").GetGuid();
        Assert.Equal((int)SubmissionStatus.Testing,
            body.RootElement.GetProperty("status").GetInt32());

        var completedSubmission = await WaitForSubmissionAsync(submissionId);
        Assert.Equal(SubmissionStatus.Finished, completedSubmission.Status);
        Assert.Equal(SharedVerdict.OK, completedSubmission.Verdict);
        await WaitForOutboxPublishedAsync(submissionId);

        await using var verifyScope = fixture.CompetitionFactory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        var run = await verifyDb.Runs.AsNoTracking()
            .SingleAsync(item => item.SubmissionId == submissionId);
        Assert.Equal(test.Id, run.TestId);
    }

    [Fact]
    public async Task Submission_IsRejectedBeforeCompetitionStarts()
    {
        var competitionId = Competition.GenerateId();
        var version = new CompetitionVersion
        {
            CompetitionId = competitionId,
            VersionNumber = 1,
            Name = "Not started",
            StartTime = DateTimeOffset.UtcNow.AddHours(1)
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
            IsPublic = true,
            Input = "input",
            Expected = "expected"
        };

        await SeedTaskAsync(competitionId, version, task, [test],
            CompetitionStatus.Published, "participant");

        using var participant = CreateClient("participant");
        using var response = await participant.PostAsJsonAsync(
            $"/internal/tasks/{task.Id}/submissions",
            new
            {
                mode = "PublicCheck",
                language = "Cpp",
                code = "int main() { return 0; }"
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task SeedTaskAsync(
        string competitionId,
        CompetitionVersion version,
        CompetitionTask task,
        IReadOnlyCollection<CompetitionTest> tests,
        CompetitionStatus status,
        string participantUserId)
    {
        await using var scope = fixture.CompetitionFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        db.Competitions.Add(new Competition
        {
            Id = competitionId,
            CreatorId = "owner",
            Status = status,
            Version = version.VersionNumber
        });
        db.CompetitionVersions.Add(version);
        db.Tasks.Add(task);
        db.Tests.AddRange(tests);
        db.Participations.Add(new Participation
        {
            CompetitionId = competitionId,
            UserId = participantUserId,
            Permission = ContestPermission.Participant
        });
        await db.SaveChangesAsync();
    }

    private async Task WaitForOutboxPublishedAsync(Guid submissionId)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            await using var scope = fixture.CompetitionFactory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
            var publishedAt = await db.SubmissionOutboxMessages.AsNoTracking()
                .Where(message => message.SubmissionId == submissionId)
                .Select(message => message.PublishedAt)
                .SingleAsync();
            if (publishedAt is not null)
                return;

            await Task.Delay(100);
        }

        throw new TimeoutException("The submission outbox entry was not published in time.");
    }

    private async Task<Submission> WaitForSubmissionAsync(Guid submissionId)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            await using var scope = fixture.CompetitionFactory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
            var submission = await db.Submissions.AsNoTracking()
                .SingleAsync(item => item.Id == submissionId);
            if (submission.Status != SubmissionStatus.Testing)
                return submission;

            await Task.Delay(100);
        }

        throw new TimeoutException("The full submission was not processed in time.");
    }

    private HttpClient CreateClient(string userId)
    {
        var client = fixture.CompetitionFactory.CreateClient(
            new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add(InternalRequestHeaders.UserId, userId);
        return client;
    }
}
