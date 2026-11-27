using BenchCode.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompetitionTask = BenchCode.CompetitionService.Entities.Task;

namespace BenchCode.CompetitionService.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal")]
public sealed class TasksReadController(CompetitionDbContext db) : ControllerBase
{
    [HttpGet("competitions/{competitionId}/tasks")]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetCompetitionTasks(
        [FromRoute] string competitionId,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var access = await db.Competitions.AsNoTracking()
            .Where(competition => competition.Id == competitionId)
            .Select(competition => new
            {
                competition.Version,
                IsOwner = competition.CreatorId == userId,
                IsParticipant = competition.Participations.Any(participation =>
                    participation.UserId == userId)
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (access is null || (!access.IsOwner && !access.IsParticipant))
            return NotFound();

        var currentVersionId = await db.CompetitionVersions.AsNoTracking()
            .Where(version => version.CompetitionId == competitionId &&
                              version.VersionNumber == access.Version)
            .Select(version => (Guid?)version.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (currentVersionId is null)
            return NotFound();

        var tasks = db.Tasks.AsNoTracking()
            .Where(task => task.CompetitionVersionId == currentVersionId.Value);
        if (!access.IsOwner)
            tasks = tasks.Where(task => !task.IsArchived);

        var result = await tasks
            .OrderBy(task => task.OrderNo)
            .Select(task => new TaskResponse(
                task.Id,
                competitionId,
                access.Version,
                task.OrderNo,
                task.Name,
                task.Description,
                task.TimeLimitMs,
                task.MemoryLimitMb,
                task.IsArchived))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("tasks/{taskId:guid}")]
    public async Task<ActionResult<TaskResponse>> GetTaskById(
        [FromRoute] Guid taskId,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var task = await db.Tasks.AsNoTracking()
            .Where(item => item.Id == taskId)
            .Select(item => new
            {
                item.Id,
                item.OrderNo,
                item.Name,
                item.Description,
                item.TimeLimitMs,
                item.MemoryLimitMb,
                item.IsArchived,
                CompetitionId = item.CompetitionVersion!.CompetitionId,
                Version = item.CompetitionVersion.VersionNumber,
                CreatorId = item.CompetitionVersion.Competition!.CreatorId,
                IsParticipant = item.CompetitionVersion.Competition.Participations.Any(
                    participation => participation.UserId == userId)
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (task is null)
            return NotFound();

        var isOwner = task.CreatorId == userId;
        if (!isOwner && (!task.IsParticipant || task.IsArchived))
            return NotFound();

        return Ok(new TaskResponse(
            task.Id,
            task.CompetitionId,
            task.Version,
            task.OrderNo,
            task.Name,
            task.Description,
            task.TimeLimitMs,
            task.MemoryLimitMb,
            task.IsArchived));
    }

    public sealed record TaskResponse(
        Guid Id,
        string CompetitionId,
        int Version,
        int OrderNo,
        string Name,
        string? Description,
        int TimeLimitMs,
        int MemoryLimitMb,
        bool IsArchived);
}
