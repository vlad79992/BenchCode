using BenchCode.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenchCode.CompetitionService.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal")]
public sealed class TestsReadController(CompetitionDbContext db) : ControllerBase
{
    [HttpGet("tasks/{taskId}/tests")]
    public async Task<ActionResult<IReadOnlyList<TestResponse>>> GetTaskTests(
        [FromRoute] Guid taskId,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var access = await db.Tasks.AsNoTracking()
            .Where(task => task.Id == taskId)
            .Select(task => new
            {
                task.IsArchived,
                CreatorId = task.CompetitionVersion!.Competition!.CreatorId,
                IsParticipant = task.CompetitionVersion.Competition.Participations.Any(
                    participation => participation.UserId == userId)
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (access is null)
            return NotFound();

        var isOwner = access.CreatorId == userId;
        if (!isOwner && (!access.IsParticipant || access.IsArchived))
            return NotFound();

        var tests = db.Tests.AsNoTracking()
            .Where(test => test.TaskId == taskId);
        if (!isOwner)
            tests = tests.Where(test => test.IsPublic);

        var result = await tests
            .OrderBy(test => test.OrderNo)
            .Select(test => new TestResponse(
                test.Id,
                test.TaskId,
                test.OrderNo,
                test.IsPublic,
                test.Input,
                test.Expected))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("tests/{testId}")]
    public async Task<ActionResult<TestResponse>> GetTestById(
        [FromRoute] Guid testId,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var test = await db.Tests.AsNoTracking()
            .Where(item => item.Id == testId)
            .Select(item => new
            {
                item.Id,
                item.TaskId,
                item.OrderNo,
                item.IsPublic,
                item.Input,
                item.Expected,
                TaskIsArchived = item.Task!.IsArchived,
                CreatorId = item.Task.CompetitionVersion!.Competition!.CreatorId,
                IsParticipant = item.Task.CompetitionVersion.Competition!.Participations.Any(
                    participation => participation.UserId == userId)
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (test is null)
            return NotFound();

        var isOwner = test.CreatorId == userId;
        if (!isOwner && (!test.IsParticipant || test.TaskIsArchived || !test.IsPublic))
            return NotFound();

        return Ok(new TestResponse(
            test.Id,
            test.TaskId,
            test.OrderNo,
            test.IsPublic,
            test.Input,
            test.Expected));
    }

    public sealed record TestResponse(
        Guid Id,
        Guid TaskId,
        int OrderNo,
        bool IsPublic,
        string Input,
        string Expected);
}
