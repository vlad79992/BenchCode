using System.ComponentModel.DataAnnotations;
using BenchCode.CompetitionService.Services;
using BenchCode.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompetitionTask = BenchCode.CompetitionService.Entities.Task;

namespace BenchCode.CompetitionService.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/competitions/{competitionId}/tasks")]
public sealed class TasksCreateController(CompetitionDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TaskCreatedResponse>> Create(
        [FromRoute] string competitionId,
        [FromBody] CreateTaskRequest request,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var competition = await db.Competitions.SingleOrDefaultAsync(
            item => item.Id == competitionId && item.CreatorId == userId,
            cancellationToken);
        if (competition is null)
            return NotFound();

        var currentVersion = await db.CompetitionVersions.AsNoTracking()
            .AsSplitQuery()
            .Include(version => version.Tasks)
            .ThenInclude(task => task.Tests)
            .SingleOrDefaultAsync(
                version => version.CompetitionId == competitionId &&
                           version.VersionNumber == competition.Version,
                cancellationToken);
        if (currentVersion is null)
            return Conflict("The current competition version was not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Name cannot be empty.");
            return ValidationProblem(ModelState);
        }

        if (currentVersion.Tasks.Any(task => task.OrderNo == request.OrderNo))
            return Conflict("Another task already uses this order number.");

        var versionClone = CompetitionVersionCloner.Clone(
            currentVersion,
            competition.Version + 1);
        var newVersion = versionClone.Version;
        var task = new CompetitionTask
        {
            CompetitionVersion = newVersion,
            OrderNo = request.OrderNo,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            TimeLimitMs = request.TimeLimitMs,
            MemoryLimitMb = request.MemoryLimitMb
        };
        newVersion.Tasks.Add(task);

        competition.Version = newVersion.VersionNumber;
        db.CompetitionVersions.Add(newVersion);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The competition was changed by another request. Reload and try again.");
        }

        return Created(
            $"/api/tasks/{task.Id}",
            new TaskCreatedResponse(
                task.Id,
                competitionId,
                newVersion.VersionNumber,
                task.OrderNo,
                task.Name,
                task.Description,
                task.TimeLimitMs,
                task.MemoryLimitMb,
                task.IsArchived));
    }

    public sealed record CreateTaskRequest(
        [Required, StringLength(200, MinimumLength = 1)] string Name,
        [Range(0, int.MaxValue)] int OrderNo,
        string? Description = null,
        [Range(1, int.MaxValue)] int TimeLimitMs = 1000,
        [Range(1, int.MaxValue)] int MemoryLimitMb = 256);

    public sealed record TaskCreatedResponse(
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
