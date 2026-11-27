using System.ComponentModel.DataAnnotations;
using BenchCode.CompetitionService.Entities;
using BenchCode.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompetitionTask = BenchCode.CompetitionService.Entities.Task;
using CompetitionTest = BenchCode.CompetitionService.Entities.Test;

namespace BenchCode.CompetitionService.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal")]
public sealed class TasksEditController(CompetitionDbContext db) : ControllerBase
{
    [HttpPatch("tasks/{taskId:guid}")]
    public async Task<ActionResult<TaskEditResponse>> Edit(
        [FromRoute] Guid taskId,
        [FromBody] UpdateTaskRequest request,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (request.Name is null && request.Description is null &&
            request.OrderNo is null && request.TimeLimitMs is null &&
            request.MemoryLimitMb is null && request.IsArchived is null)
        {
            return BadRequest("At least one task field must be provided.");
        }

        var location = await GetTaskLocationAsync(taskId, cancellationToken);
        if (location is null)
            return NotFound();

        var competition = await db.Competitions.SingleOrDefaultAsync(
            item => item.Id == location.CompetitionId && item.CreatorId == userId,
            cancellationToken);
        if (competition is null || competition.Version != location.VersionNumber)
            return NotFound();

        var currentVersion = await LoadVersionAsync(
            location.CompetitionVersionId,
            cancellationToken);
        if (currentVersion is null)
            return NotFound();

        var currentTask = currentVersion.Tasks.SingleOrDefault(item => item.Id == taskId);
        if (currentTask is null)
            return NotFound();

        if (request.Name is not null && string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Name cannot be empty.");
            return ValidationProblem(ModelState);
        }

        var newOrderNo = request.OrderNo ?? currentTask.OrderNo;
        if (currentVersion.Tasks.Any(item => item.Id != taskId && item.OrderNo == newOrderNo))
            return Conflict("Another task already uses this order number.");

        var newVersion = CloneVersion(
            currentVersion,
            competition.Version + 1,
            excludedTaskId: null,
            out var taskCopies);
        var updatedTask = taskCopies[taskId];

        if (request.Name is not null)
            updatedTask.Name = request.Name.Trim();
        if (request.Description is not null)
            updatedTask.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();
        if (request.OrderNo is not null)
            updatedTask.OrderNo = request.OrderNo.Value;
        if (request.TimeLimitMs is not null)
            updatedTask.TimeLimitMs = request.TimeLimitMs.Value;
        if (request.MemoryLimitMb is not null)
            updatedTask.MemoryLimitMb = request.MemoryLimitMb.Value;
        if (request.IsArchived is not null)
            updatedTask.IsArchived = request.IsArchived.Value;

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

        return Ok(ToResponse(updatedTask, newVersion));
    }

    [HttpDelete("tasks/{taskId:guid}")]
    public async Task<ActionResult<CompetitionVersionChangedResponse>> Delete(
        [FromRoute] Guid taskId,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var location = await GetTaskLocationAsync(taskId, cancellationToken);
        if (location is null)
            return NotFound();

        var competition = await db.Competitions.SingleOrDefaultAsync(
            item => item.Id == location.CompetitionId && item.CreatorId == userId,
            cancellationToken);
        if (competition is null || competition.Version != location.VersionNumber)
            return NotFound();

        var currentVersion = await LoadVersionAsync(
            location.CompetitionVersionId,
            cancellationToken);
        if (currentVersion is null || currentVersion.Tasks.All(item => item.Id != taskId))
            return NotFound();

        var newVersion = CloneVersion(
            currentVersion,
            competition.Version + 1,
            excludedTaskId: taskId,
            out _);

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

        return Ok(new CompetitionVersionChangedResponse(
            competition.Id,
            newVersion.VersionNumber));
    }

    private async Task<TaskLocation?> GetTaskLocationAsync(
        Guid taskId,
        CancellationToken cancellationToken) =>
        await db.Tasks.AsNoTracking()
            .Where(task => task.Id == taskId)
            .Select(task => new TaskLocation(
                task.CompetitionVersionId,
                task.CompetitionVersion!.CompetitionId,
                task.CompetitionVersion.VersionNumber))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<CompetitionVersion?> LoadVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken) =>
        await db.CompetitionVersions.AsNoTracking()
            .AsSplitQuery()
            .Include(version => version.Tasks)
            .ThenInclude(task => task.Tests)
            .SingleOrDefaultAsync(version => version.Id == versionId, cancellationToken);

    private static CompetitionVersion CloneVersion(
        CompetitionVersion source,
        int versionNumber,
        Guid? excludedTaskId,
        out Dictionary<Guid, CompetitionTask> taskCopies)
    {
        var copy = new CompetitionVersion
        {
            CompetitionId = source.CompetitionId,
            VersionNumber = versionNumber,
            Name = source.Name,
            Description = source.Description,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            AllowViewOthersCode = source.AllowViewOthersCode
        };
        taskCopies = [];

        foreach (var oldTask in source.Tasks.OrderBy(task => task.OrderNo))
        {
            if (oldTask.Id == excludedTaskId)
                continue;

            var newTask = new CompetitionTask
            {
                CompetitionVersion = copy,
                OrderNo = oldTask.OrderNo,
                Name = oldTask.Name,
                Description = oldTask.Description,
                TimeLimitMs = oldTask.TimeLimitMs,
                MemoryLimitMb = oldTask.MemoryLimitMb,
                IsArchived = oldTask.IsArchived
            };

            foreach (var oldTest in oldTask.Tests.OrderBy(test => test.OrderNo))
            {
                newTask.Tests.Add(new CompetitionTest
                {
                    Task = newTask,
                    OrderNo = oldTest.OrderNo,
                    IsPublic = oldTest.IsPublic,
                    Input = oldTest.Input,
                    Expected = oldTest.Expected
                });
            }

            copy.Tasks.Add(newTask);
            taskCopies.Add(oldTask.Id, newTask);
        }

        return copy;
    }

    private static TaskEditResponse ToResponse(
        CompetitionTask task,
        CompetitionVersion version) =>
        new(
            task.Id,
            version.CompetitionId,
            version.VersionNumber,
            task.OrderNo,
            task.Name,
            task.Description,
            task.TimeLimitMs,
            task.MemoryLimitMb,
            task.IsArchived);

    private sealed record TaskLocation(
        Guid CompetitionVersionId,
        string CompetitionId,
        int VersionNumber);

    public sealed record UpdateTaskRequest(
        [StringLength(200, MinimumLength = 1)] string? Name = null,
        string? Description = null,
        [Range(0, int.MaxValue)] int? OrderNo = null,
        [Range(1, int.MaxValue)] int? TimeLimitMs = null,
        [Range(1, int.MaxValue)] int? MemoryLimitMb = null,
        bool? IsArchived = null);

    public sealed record TaskEditResponse(
        Guid Id,
        string CompetitionId,
        int Version,
        int OrderNo,
        string Name,
        string? Description,
        int TimeLimitMs,
        int MemoryLimitMb,
        bool IsArchived);

    public sealed record CompetitionVersionChangedResponse(
        string CompetitionId,
        int Version);
}
