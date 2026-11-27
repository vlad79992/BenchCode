extern alias CompetitionService;
using BenchCode.Shared;
using SharedVerdict = BenchCode.Shared.Entities.Verdict;
using Competition = CompetitionService::BenchCode.CompetitionService.Entities.Competition;
using CompetitionStatus = CompetitionService::BenchCode.CompetitionService.Entities.CompetitionStatus;
using CompetitionLanguage = CompetitionService::BenchCode.CompetitionService.Entities.Language;
using CompetitionTask = CompetitionService::BenchCode.CompetitionService.Entities.Task;
using CompetitionTest = CompetitionService::BenchCode.CompetitionService.Entities.Test;
using Submission = CompetitionService::BenchCode.CompetitionService.Entities.Submission;
using SubmissionStatus = CompetitionService::BenchCode.CompetitionService.Entities.SubmissionStatus;
using CompetitionQueue = CompetitionService::BenchCode.CompetitionService.Services.ISubmissionQueue;
using SubmissionTestCase = CompetitionService::BenchCode.CompetitionService.Services.SubmissionTestCase;
using PublicCheckMessage = CompetitionService::BenchCode.CompetitionService.Services.PublicCheckMessage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CompetitionDbContext = CompetitionService::BenchCode.CompetitionService.CompetitionDbContext;

namespace BenchCode.Test.IntegrationTests;

[Collection("AuthTests")]
public sealed class CompetitionSubmissionQueueIntegrationTests(Fixture fixture)
{
    [Fact]
    public async Task PublicCheck_UsesRequestReplyWithoutPersistingSubmission()
    {
        var services = fixture.CompetitionFactory.Services;
        var queue = services.GetRequiredService<CompetitionQueue>();
        var testId = Guid.CreateVersion7();

        await using var beforeScope = services.CreateAsyncScope();
        var beforeDb = beforeScope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        var submissionsBefore = await beforeDb.Submissions.CountAsync();

        var response = await queue.RunPublicCheckAsync(
            new PublicCheckMessage(
                CompetitionLanguage.Cpp,
                "int main() { return 0; }",
                [new SubmissionTestCase(testId, 7, "input", "expected")]),
            CancellationToken.None);

        Assert.True(response.Completed);
        Assert.True(response.Built);
        Assert.Equal((int)SharedVerdict.OK, (int)response.FinalVerdict!.Value);
        var run = Assert.Single(response.Runs);
        Assert.Equal(testId, run.TestId);
        Assert.Equal(7, run.TestOrderNo);
        Assert.Equal(submissionsBefore, await beforeDb.Submissions.CountAsync());
    }

    [Fact]
    public async Task FullSubmission_IsAcknowledgedAfterItsRunIsPersisted()
    {
        var competitionId = Competition.GenerateId();
        var version = new CompetitionService::BenchCode.CompetitionService.Entities.CompetitionVersion
        {
            CompetitionId = competitionId,
            VersionNumber = 1,
            Name = "Queue test"
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
        var submission = new Submission
        {
            CompetitionId = competitionId,
            TaskId = task.Id,
            UserId = "participant",
            Language = CompetitionLanguage.Cpp,
            Code = "int main() { return 0; }",
            CompetitionVersion = 1,
            Status = SubmissionStatus.Testing
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
            db.Submissions.Add(submission);
            await db.SaveChangesAsync();
        }

        var queue = fixture.CompetitionFactory.Services.GetRequiredService<CompetitionQueue>();
        await queue.EnqueueFullSubmissionAsync(submission.Id, CancellationToken.None);

        var completed = await WaitForSubmissionAsync(submission.Id);
        Assert.Equal(SubmissionStatus.Finished, completed.Status);
        Assert.Equal(SharedVerdict.OK, completed.Verdict);

        await using var verifyScope = fixture.CompetitionFactory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        var run = await verifyDb.Runs.AsNoTracking()
            .SingleAsync(item => item.SubmissionId == submission.Id);
        Assert.Equal(test.Id, run.TestId);
        Assert.Equal(SharedVerdict.OK, run.Verdict);
        Assert.Equal("test output", run.Stdout);
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

        throw new TimeoutException("The full-submission queue did not finish processing in time.");
    }
}
