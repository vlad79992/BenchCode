using System.ComponentModel.DataAnnotations;
using BenchCode.CompetitionService.Entities;
using BenchCode.CompetitionService.Services;
using BenchCode.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompetitionLanguage = BenchCode.CompetitionService.Entities.Language;
using CompetitionSubmission = BenchCode.CompetitionService.Entities.Submission;

namespace BenchCode.CompetitionService.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal")]
public sealed class SubmissionsCreateController(
    CompetitionDbContext db,
    ISubmissionQueue submissionQueue,
    ILogger<SubmissionsCreateController> logger) : ControllerBase
{
    [HttpPost("tasks/{taskId}/submissions")]
    public async Task<IActionResult> Create(
        [FromRoute] Guid taskId,
        [FromBody] CreateSubmissionRequest request,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (!TryParseMode(request.Mode, out var mode))
            return BadRequest("Mode must be 'PublicCheck' or 'FullSubmission'.");

        if (!TryParseLanguage(request.Language, out var language))
            return BadRequest("Language must be Cpp, CSharp, Java, or Python.");
        if (language == CompetitionLanguage.Python)
            return BadRequest("Python is not supported by ProgramRunner yet.");

        var task = await db.Tasks.AsNoTracking()
            .Where(item => item.Id == taskId)
            .Select(item => new TaskLocation(
                item.IsArchived,
                item.CompetitionVersion!.CompetitionId,
                item.CompetitionVersion.VersionNumber,
                item.CompetitionVersion.Competition!.Version,
                item.CompetitionVersion.Competition.CreatorId,
                item.CompetitionVersion.Competition.Status,
                item.CompetitionVersion.StartTime,
                item.CompetitionVersion.EndTime,
                item.CompetitionVersion.Competition.Participations.Any(
                    participation => participation.UserId == userId)))
            .SingleOrDefaultAsync(cancellationToken);
        if (task is null || task.VersionNumber != task.CurrentVersion)
            return NotFound();

        var isOwner = task.CreatorId == userId;
        if (!isOwner && !task.IsParticipant)
            return NotFound();

        var now = DateTimeOffset.UtcNow;
        if (task.StartTime is not null && now < task.StartTime.Value)
            return Conflict("The competition has not started yet.");

        if (mode == SubmissionMode.FullSubmission)
        {
            if (task.CompetitionStatus != CompetitionStatus.Published || task.IsArchived)
                return Conflict("Full submissions are accepted only for active, non-archived tasks.");

            if (task.EndTime is not null && now >= task.EndTime.Value)
                return Conflict("The full-submission period has ended.");
        }


        if (mode == SubmissionMode.PublicCheck)
        {
            var publicTests = await db.Tests.AsNoTracking()
                .Where(test => test.TaskId == taskId && test.IsPublic)
                .OrderBy(test => test.OrderNo)
                .Select(test => new SubmissionTestCase(
                    test.Id,
                    test.OrderNo,
                    test.Input,
                    test.Expected))
                .ToListAsync(cancellationToken);
            if (publicTests.Count == 0)
                return Conflict("The task has no public tests to run.");

            try
            {
                var result = await submissionQueue.RunPublicCheckAsync(
                    new PublicCheckMessage(language, request.Code, publicTests),
                    cancellationToken);
                return result.Completed ? Ok(result) : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
            }
            catch (TimeoutException)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout,
                    "The public check did not finish before the response timeout.");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Could not complete public check for task {TaskId}.", taskId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "The public-check service is temporarily unavailable.");
            }
        }

        var tests = await db.Tests.AsNoTracking()
            .Where(test => test.TaskId == taskId)
            .OrderBy(test => test.OrderNo)
            .Select(test => new SubmissionTestCase(
                test.Id,
                test.OrderNo,
                test.Input,
                test.Expected))
            .ToListAsync(cancellationToken);
        if (tests.Count == 0)
            return Conflict("The task has no tests to run.");

        var submission = new CompetitionSubmission
        {
            CompetitionId = task.CompetitionId,
            TaskId = taskId,
            UserId = userId,
            Language = language,
            Code = request.Code,
            CompetitionVersion = task.VersionNumber,
            Status = SubmissionStatus.Testing
        };
        db.Submissions.Add(submission);
        db.SubmissionOutboxMessages.Add(new SubmissionOutboxMessage
        {
            SubmissionId = submission.Id
        });
        await db.SaveChangesAsync(cancellationToken);

        return Accepted(
            $"/api/submissions/{submission.Id}",
            new FullSubmissionResponse(
                submission.Id,
                submission.CompetitionId,
                submission.TaskId,
                submission.CompetitionVersion,
                submission.Status,
                submission.SubmittedAt));
    }

    private static bool TryParseMode(string mode, out SubmissionMode submissionMode)
    {
        switch (mode.Trim().ToLowerInvariant())
        {
            case "publiccheck":
                submissionMode = SubmissionMode.PublicCheck;
                return true;
            case "fullsubmission":
                submissionMode = SubmissionMode.FullSubmission;
                return true;
            default:
                submissionMode = default;
                return false;
        }
    }

    private static bool TryParseLanguage(string language, out CompetitionLanguage parsedLanguage)
    {
        switch (language.Trim().ToLowerInvariant())
        {
            case "cpp":
                parsedLanguage = CompetitionLanguage.Cpp;
                return true;
            case "csharp":
                parsedLanguage = CompetitionLanguage.CSharp;
                return true;
            case "java":
                parsedLanguage = CompetitionLanguage.Java;
                return true;
            case "python":
                parsedLanguage = CompetitionLanguage.Python;
                return true;
            default:
                parsedLanguage = default;
                return false;
        }
    }

    private enum SubmissionMode
    {
        PublicCheck,
        FullSubmission
    }

    private sealed record TaskLocation(
        bool IsArchived,
        string CompetitionId,
        int VersionNumber,
        int CurrentVersion,
        string CreatorId,
        CompetitionStatus CompetitionStatus,
        DateTimeOffset? StartTime,
        DateTimeOffset? EndTime,
        bool IsParticipant);

    public sealed record CreateSubmissionRequest(
        [Required] string Mode,
        [Required] string Language,
        [Required, StringLength(65_536, MinimumLength = 1)] string Code);

    public sealed record FullSubmissionResponse(
        Guid Id,
        string CompetitionId,
        Guid TaskId,
        int CompetitionVersion,
        SubmissionStatus Status,
        DateTimeOffset SubmittedAt);
}
