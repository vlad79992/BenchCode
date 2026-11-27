using System.ComponentModel.DataAnnotations;
using BenchCode.CompetitionService.Services;
using BenchCode.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompetitionTest = BenchCode.CompetitionService.Entities.Test;

namespace BenchCode.CompetitionService.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal")]
public sealed class TestsCreateController(CompetitionDbContext db) : ControllerBase
{
    [HttpPost("tasks/{taskId}/tests")]
    public async Task<ActionResult<TestCreatedResponse>> Create(
        [FromRoute] Guid taskId,
        [FromBody] CreateTestRequest request,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var location = await db.Tasks.AsNoTracking()
            .Where(task => task.Id == taskId)
            .Select(task => new TaskLocation(
                task.CompetitionVersionId,
                task.CompetitionVersion!.CompetitionId,
                task.CompetitionVersion.VersionNumber))
            .SingleOrDefaultAsync(cancellationToken);
        if (location is null)
            return NotFound();

        var competition = await db.Competitions.SingleOrDefaultAsync(
            item => item.Id == location.CompetitionId && item.CreatorId == userId,
            cancellationToken);
        if (competition is null || competition.Version != location.VersionNumber)
            return NotFound();

        var currentVersion = await db.CompetitionVersions.AsNoTracking()
            .AsSplitQuery()
            .Include(version => version.Tasks)
            .ThenInclude(task => task.Tests)
            .SingleOrDefaultAsync(
                version => version.Id == location.CompetitionVersionId &&
                           version.CompetitionId == location.CompetitionId &&
                           version.VersionNumber == competition.Version,
                cancellationToken);
        if (currentVersion is null)
            return Conflict("The current competition version was not found.");

        var currentTask = currentVersion.Tasks.SingleOrDefault(task => task.Id == taskId);
        if (currentTask is null)
            return NotFound();

        if (currentTask.Tests.Any(test => test.OrderNo == request.OrderNo))
            return Conflict("Another test already uses this order number.");

        var versionClone = CompetitionVersionCloner.Clone(
            currentVersion,
            competition.Version + 1);
        var newVersion = versionClone.Version;
        var newTask = versionClone.TaskCopies[taskId];
        var test = new CompetitionTest
        {
            Task = newTask,
            OrderNo = request.OrderNo,
            IsPublic = request.IsPublic,
            Input = request.Input,
            Expected = request.Expected
        };
        newTask.Tests.Add(test);

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
            $"/api/tests/{test.Id}",
            new TestCreatedResponse(
                test.Id,
                location.CompetitionId,
                newVersion.VersionNumber,
                newTask.Id,
                test.OrderNo,
                test.IsPublic,
                test.Input,
                test.Expected));
    }

    private sealed record TaskLocation(
        Guid CompetitionVersionId,
        string CompetitionId,
        int VersionNumber);

    public sealed record CreateTestRequest(
        [Range(0, int.MaxValue)] int OrderNo,
        [Required(AllowEmptyStrings = true)] string Input,
        [Required(AllowEmptyStrings = true)] string Expected,
        bool IsPublic = false);

    public sealed record TestCreatedResponse(
        Guid Id,
        string CompetitionId,
        int Version,
        Guid TaskId,
        int OrderNo,
        bool IsPublic,
        string Input,
        string Expected);
}
