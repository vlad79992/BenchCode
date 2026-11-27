using System.Text.Json.Serialization;
using BenchCode.CompetitionService.Entities;
using BenchCode.Shared;
using BenchCode.Shared.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenchCode.CompetitionService.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal")]
public sealed class SubmissionsReadController(CompetitionDbContext db) : ControllerBase
{
    [HttpGet("tasks/{taskId}/submissions")]
    public async Task<ActionResult<IReadOnlyList<SubmissionListItem>>> GetTaskSubmissions(
        [FromRoute] Guid taskId,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var taskAccess = await db.Tasks.AsNoTracking()
            .Where(task => task.Id == taskId)
            .Select(task => new
            {
                task.IsArchived,
                CompetitionId = task.CompetitionVersion!.CompetitionId,
                CreatorId = task.CompetitionVersion.Competition!.CreatorId,
                AllowViewOthersCode = task.CompetitionVersion.AllowViewOthersCode,
                IsParticipant = task.CompetitionVersion.Competition.Participations.Any(
                    participation => participation.UserId == userId)
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (taskAccess is null)
            return NotFound();

        var isOwner = taskAccess.CreatorId == userId;
        if (!isOwner && (!taskAccess.IsParticipant || taskAccess.IsArchived))
            return NotFound();

        var submissions = db.Submissions.AsNoTracking()
            .Where(submission => submission.TaskId == taskId);
        if (!isOwner)
        {
            if (taskAccess.AllowViewOthersCode)
            {
                submissions = submissions.Where(submission =>
                    submission.UserId == userId ||
                    db.Participations.Any(participation =>
                        participation.CompetitionId == taskAccess.CompetitionId &&
                        participation.UserId == submission.UserId &&
                        !participation.IsHidden));
            }
            else
            {
                submissions = submissions.Where(submission => submission.UserId == userId);
            }
        }

        var result = await submissions
            .OrderByDescending(submission => submission.SubmittedAt)
            .Select(submission => new SubmissionListItem(
                submission.Id,
                submission.TaskId,
                submission.UserId,
                submission.Language,
                submission.SubmittedAt,
                submission.CompetitionVersion,
                submission.Status,
                submission.Verdict))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("submissions/{submissionId}")]
    public async Task<ActionResult<SubmissionDetails>> GetSubmissionById(
        [FromRoute] Guid submissionId,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var access = await GetSubmissionAccessAsync(submissionId, userId, cancellationToken);
        if (access is null || !CanViewSubmission(access, userId))
            return NotFound();

        var submission = await db.Submissions.AsNoTracking()
            .Where(item => item.Id == submissionId)
            .Select(item => new SubmissionDetails(
                item.Id,
                item.CompetitionId,
                item.TaskId,
                item.UserId,
                item.Language,
                item.Code,
                item.SubmittedAt,
                item.CompetitionVersion,
                item.Status,
                item.BuildError,
                item.Verdict))
            .SingleOrDefaultAsync(cancellationToken);
        return submission is null ? NotFound() : Ok(submission);
    }

    [HttpGet("submissions/{submissionId}/runs")]
    public async Task<ActionResult<SubmissionRunResults>> GetSubmissionRuns(
        [FromRoute] Guid submissionId,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var access = await GetSubmissionAccessAsync(submissionId, userId, cancellationToken);
        if (access is null || !CanViewSubmission(access, userId))
            return NotFound();

        var isOwner = access.CreatorId == userId;
        var runsQuery = db.Runs.AsNoTracking()
            .Where(run => run.SubmissionId == submissionId);
        if (!isOwner && access.SubmitterId != userId)
            runsQuery = runsQuery.Where(run => run.Test!.IsPublic);

        var runs = await runsQuery
            .OrderBy(run => run.Test!.OrderNo)
            .Select(run => new
            {
                run.Id,
                run.TestId,
                TestOrderNo = run.Test!.OrderNo,
                IsPublic = run.Test.IsPublic,
                run.Verdict,
                run.Time,
                run.ExitCode
            })
            .ToListAsync(cancellationToken);

        var items = runs.Select(run =>
        {
            var canSeeDetails = isOwner || run.IsPublic;
            return new SubmissionRunItem(
                canSeeDetails ? run.Id : null,
                canSeeDetails ? run.TestId : null,
                run.TestOrderNo,
                run.Verdict,
                canSeeDetails ? run.Time : null,
                canSeeDetails ? run.ExitCode : null);
        }).ToArray();

        return Ok(new SubmissionRunResults(
            submissionId,
            access.Status,
            access.Verdict,
            items));
    }

    [HttpGet("submissions/{submissionId}/runs/{runId}")]
    public async Task<ActionResult<RunDetails>> GetRunDetails(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid runId,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var access = await GetSubmissionAccessAsync(submissionId, userId, cancellationToken);
        if (access is null || !CanViewSubmission(access, userId))
            return NotFound();

        var run = await db.Runs.AsNoTracking()
            .Where(item => item.Id == runId && item.SubmissionId == submissionId)
            .Select(item => new
            {
                item.Id,
                item.TestId,
                TestOrderNo = item.Test!.OrderNo,
                IsPublic = item.Test.IsPublic,
                item.Verdict,
                item.Time,
                item.ExitCode,
                item.Stdout,
                item.Stderr,
                item.Test.Input,
                item.Test.Expected
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (run is null)
            return NotFound();

        if (run.IsPublic is false && access.CreatorId != userId)
            return NotFound();

        return Ok(new RunDetails(
            run.Id,
            run.TestId,
            run.TestOrderNo,
            run.Verdict,
            run.Time,
            run.ExitCode,
            run.Input,
            run.Expected,
            run.Stdout,
            run.Stderr));
    }

    private async Task<SubmissionAccess?> GetSubmissionAccessAsync(
        Guid submissionId,
        string userId,
        CancellationToken cancellationToken) =>
        await db.Submissions.AsNoTracking()
            .Where(submission => submission.Id == submissionId)
            .Select(submission => new SubmissionAccess(
                submission.UserId,
                submission.Status,
                submission.Verdict,
                submission.Task!.IsArchived,
                submission.Task.CompetitionVersion!.AllowViewOthersCode,
                submission.Competition!.CreatorId,
                submission.Competition.Participations.Any(
                    participation => participation.UserId == userId),
                submission.Competition.Participations.Any(
                    participation => participation.UserId == submission.UserId),
                submission.Competition.Participations.Any(
                    participation => participation.UserId == submission.UserId &&
                                     participation.IsHidden)))
            .SingleOrDefaultAsync(cancellationToken);

    private static bool CanViewSubmission(SubmissionAccess access, string userId)
    {
        if (access.CreatorId == userId)
            return true;

        if (!access.IsParticipant || access.TaskIsArchived)
            return false;

        if (access.SubmitterId == userId)
            return true;

        return access.AllowViewOthersCode &&
               access.IsSubmitterParticipant &&
               !access.SubmitterIsHidden;
    }

    public sealed record SubmissionListItem(
        Guid Id,
        Guid TaskId,
        string UserId,
        Language Language,
        DateTimeOffset SubmittedAt,
        int CompetitionVersion,
        SubmissionStatus Status,
        Verdict? Verdict);

    public sealed record SubmissionDetails(
        Guid Id,
        string CompetitionId,
        Guid TaskId,
        string UserId,
        Language Language,
        string Code,
        DateTimeOffset SubmittedAt,
        int CompetitionVersion,
        SubmissionStatus Status,
        string? BuildError,
        Verdict? Verdict);

    public sealed record SubmissionRunResults(
        Guid SubmissionId,
        SubmissionStatus Status,
        Verdict? Verdict,
        IReadOnlyList<SubmissionRunItem> Runs);

    public sealed record SubmissionRunItem(
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? RunId,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? TestId,
        int TestOrderNo,
        Verdict Verdict,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TimeSpan? Time,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? ExitCode);

    public sealed record RunDetails(
        Guid RunId,
        Guid TestId,
        int TestOrderNo,
        Verdict Verdict,
        TimeSpan Time,
        long ExitCode,
        string Input,
        string Expected,
        string Stdout,
        string Stderr);

    private sealed record SubmissionAccess(
        string SubmitterId,
        SubmissionStatus Status,
        Verdict? Verdict,
        bool TaskIsArchived,
        bool AllowViewOthersCode,
        string CreatorId,
        bool IsParticipant,
        bool IsSubmitterParticipant,
        bool SubmitterIsHidden);
}
