extern alias CompetitionService;
using BenchCode.Shared;
using SharedVerdict = BenchCode.Shared.Entities.Verdict;
using Competition = CompetitionService::BenchCode.CompetitionService.Entities.Competition;
using CompetitionStatus = CompetitionService::BenchCode.CompetitionService.Entities.CompetitionStatus;
using ContestPermission = CompetitionService::BenchCode.CompetitionService.Entities.ContestPermission;
using CompetitionTask = CompetitionService::BenchCode.CompetitionService.Entities.Task;
using CompetitionTest = CompetitionService::BenchCode.CompetitionService.Entities.Test;
using Submission = CompetitionService::BenchCode.CompetitionService.Entities.Submission;
using SubmissionStatus = CompetitionService::BenchCode.CompetitionService.Entities.SubmissionStatus;
using Language = CompetitionService::BenchCode.CompetitionService.Entities.Language;
using Run = CompetitionService::BenchCode.CompetitionService.Entities.Run;
using Participation = CompetitionService::BenchCode.CompetitionService.Entities.Participation;
using CompetitionVersion = CompetitionService::BenchCode.CompetitionService.Entities.CompetitionVersion;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using CompetitionDbContext = CompetitionService::BenchCode.CompetitionService.CompetitionDbContext;

namespace BenchCode.Test.IntegrationTests;

[Collection("AuthTests")]
public sealed class CompetitionSubmissionsReadIntegrationTests(Fixture fixture)
{
    [Fact]
    public async Task SubmissionReads_RespectCodeAndHiddenTestVisibility()
    {
        var competitionId = Competition.GenerateId();
        var version = new CompetitionVersion
        {
            CompetitionId = competitionId,
            VersionNumber = 1,
            Name = "Competition",
            AllowViewOthersCode = true
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
        var submission = new Submission
        {
            CompetitionId = competitionId,
            TaskId = task.Id,
            UserId = "submitter",
            Language = Language.CSharp,
            Code = "public class Program {}",
            CompetitionVersion = 1,
            Status = SubmissionStatus.Finished,
            Verdict = SharedVerdict.WA
        };
        var hiddenAuthorSubmission = new Submission
        {
            CompetitionId = competitionId,
            TaskId = task.Id,
            UserId = "hidden-author",
            Language = Language.Python,
            Code = "print('secret')",
            CompetitionVersion = 1,
            Status = SubmissionStatus.Finished,
            Verdict = SharedVerdict.OK
        };
        var publicRun = new Run
        {
            SubmissionId = submission.Id,
            TestId = publicTest.Id,
            Verdict = SharedVerdict.OK,
            Stdout = "public stdout",
            Stderr = "public stderr",
            ExitCode = 0,
            Time = TimeSpan.FromMilliseconds(4)
        };
        var hiddenRun = new Run
        {
            SubmissionId = submission.Id,
            TestId = hiddenTest.Id,
            Verdict = SharedVerdict.WA,
            Stdout = "hidden stdout",
            Stderr = "hidden stderr",
            ExitCode = 1,
            Time = TimeSpan.FromMilliseconds(8)
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
            db.Tests.AddRange(publicTest, hiddenTest);
            db.Submissions.AddRange(submission, hiddenAuthorSubmission);
            db.Runs.AddRange(publicRun, hiddenRun);
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
                    UserId = "submitter"
                },
                new Participation
                {
                    CompetitionId = competitionId,
                    UserId = "viewer"
                },
                new Participation
                {
                    CompetitionId = competitionId,
                    UserId = "hidden-author",
                    IsHidden = true
                });
            await db.SaveChangesAsync();
        }

        using var viewer = CreateClient("viewer");
        using var listResponse = await viewer.GetAsync($"/internal/tasks/{task.Id}/submissions");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        using var listBody = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Single(listBody.RootElement.EnumerateArray());
        Assert.Equal(
            submission.Id.ToString(),
            listBody.RootElement[0].GetProperty("id").GetString());

        using var visibleSubmissionResponse = await viewer.GetAsync($"/internal/submissions/{submission.Id}");
        Assert.Equal(HttpStatusCode.OK, visibleSubmissionResponse.StatusCode);
        using var visibleSubmissionBody = JsonDocument.Parse(
            await visibleSubmissionResponse.Content.ReadAsStringAsync());
        Assert.Equal("public class Program {}", visibleSubmissionBody.RootElement.GetProperty("code").GetString());

        using var hiddenSubmissionResponse = await viewer.GetAsync(
            $"/internal/submissions/{hiddenAuthorSubmission.Id}");
        Assert.Equal(HttpStatusCode.NotFound, hiddenSubmissionResponse.StatusCode);

        using var viewerRunsResponse = await viewer.GetAsync(
            $"/internal/submissions/{submission.Id}/runs");
        Assert.Equal(HttpStatusCode.OK, viewerRunsResponse.StatusCode);
        using var viewerRunsBody = JsonDocument.Parse(
            await viewerRunsResponse.Content.ReadAsStringAsync());
        var viewerRuns = viewerRunsBody.RootElement.GetProperty("runs").EnumerateArray().ToArray();
        Assert.Single(viewerRuns);
        Assert.Equal(1, viewerRuns[0].GetProperty("testOrderNo").GetInt32());

        using var submitter = CreateClient("submitter");
        using var runsResponse = await submitter.GetAsync($"/internal/submissions/{submission.Id}/runs");
        Assert.Equal(HttpStatusCode.OK, runsResponse.StatusCode);
        using var runsBody = JsonDocument.Parse(await runsResponse.Content.ReadAsStringAsync());
        var hiddenRunSummary = runsBody.RootElement.GetProperty("runs")
            .EnumerateArray()
            .Single(item => item.GetProperty("testOrderNo").GetInt32() == 2);
        Assert.Equal((int)SharedVerdict.WA, hiddenRunSummary.GetProperty("verdict").GetInt32());
        Assert.False(hiddenRunSummary.TryGetProperty("runId", out _));
        Assert.False(hiddenRunSummary.TryGetProperty("time", out _));

        using var hiddenRunDetailsResponse = await submitter.GetAsync(
            $"/internal/submissions/{submission.Id}/runs/{hiddenRun.Id}");
        Assert.Equal(HttpStatusCode.NotFound, hiddenRunDetailsResponse.StatusCode);

        using var publicRunDetailsResponse = await viewer.GetAsync(
            $"/internal/submissions/{submission.Id}/runs/{publicRun.Id}");
        Assert.Equal(HttpStatusCode.OK, publicRunDetailsResponse.StatusCode);
        using var publicRunDetailsBody = JsonDocument.Parse(
            await publicRunDetailsResponse.Content.ReadAsStringAsync());
        Assert.Equal("public input", publicRunDetailsBody.RootElement.GetProperty("input").GetString());
        Assert.Equal("public expected", publicRunDetailsBody.RootElement.GetProperty("expected").GetString());
        Assert.Equal("public stdout", publicRunDetailsBody.RootElement.GetProperty("stdout").GetString());

        using var owner = CreateClient("owner");
        using var hiddenRunForOwner = await owner.GetAsync(
            $"/internal/submissions/{submission.Id}/runs/{hiddenRun.Id}");
        Assert.Equal(HttpStatusCode.OK, hiddenRunForOwner.StatusCode);
        using var hiddenRunForOwnerBody = JsonDocument.Parse(
            await hiddenRunForOwner.Content.ReadAsStringAsync());
        Assert.Equal("hidden input", hiddenRunForOwnerBody.RootElement.GetProperty("input").GetString());
        Assert.Equal("hidden expected", hiddenRunForOwnerBody.RootElement.GetProperty("expected").GetString());
        Assert.Equal("hidden stdout", hiddenRunForOwnerBody.RootElement.GetProperty("stdout").GetString());
    }

    [Fact]
    public async Task SubmissionReads_RestrictCodeWhenCompetitionDisallowsIt()
    {
        var competitionId = Competition.GenerateId();
        var version = new CompetitionVersion
        {
            CompetitionId = competitionId,
            VersionNumber = 1,
            Name = "Private code competition",
            AllowViewOthersCode = false
        };
        var task = new CompetitionTask
        {
            CompetitionVersionId = version.Id,
            OrderNo = 1,
            Name = "Task"
        };
        var writerSubmission = new Submission
        {
            CompetitionId = competitionId,
            TaskId = task.Id,
            UserId = "writer",
            Language = Language.Cpp,
            Code = "int main() {}",
            CompetitionVersion = 1
        };
        var viewerSubmission = new Submission
        {
            CompetitionId = competitionId,
            TaskId = task.Id,
            UserId = "viewer",
            Language = Language.Cpp,
            Code = "int main() { return 0; }",
            CompetitionVersion = 1
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
            db.Submissions.AddRange(writerSubmission, viewerSubmission);
            db.Participations.AddRange(
                new Participation { CompetitionId = competitionId, UserId = "writer" },
                new Participation { CompetitionId = competitionId, UserId = "viewer" });
            await db.SaveChangesAsync();
        }

        using var viewer = CreateClient("viewer");
        using var listResponse = await viewer.GetAsync($"/internal/tasks/{task.Id}/submissions");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        using var listBody = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Single(listBody.RootElement.EnumerateArray());
        Assert.Equal(
            viewerSubmission.Id.ToString(),
            listBody.RootElement[0].GetProperty("id").GetString());

        using var otherSubmissionResponse = await viewer.GetAsync(
            $"/internal/submissions/{writerSubmission.Id}");
        Assert.Equal(HttpStatusCode.NotFound, otherSubmissionResponse.StatusCode);

        using var writer = CreateClient("writer");
        using var ownSubmissionResponse = await writer.GetAsync(
            $"/internal/submissions/{writerSubmission.Id}");
        Assert.Equal(HttpStatusCode.OK, ownSubmissionResponse.StatusCode);
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
