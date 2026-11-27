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
[Route("internal/competitions")]
public sealed class CompetitionEditController(CompetitionDbContext db) : ControllerBase
{
    [HttpPatch("{id}")]
    public async Task<ActionResult<CompetitionEditResponse>> Edit(
        [FromRoute] string id,
        [FromBody] UpdateCompetitionRequest request,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (request.Name is null && request.Description is null &&
            request.StartTime is null && request.EndTime is null &&
            request.AllowViewOthersCode is null)
        {
            return BadRequest("At least one competition field must be provided.");
        }

        var competition = await db.Competitions.SingleOrDefaultAsync(
            item => item.Id == id && item.CreatorId == userId,
            cancellationToken);
        if (competition is null)
            return NotFound();

        var currentVersion = await db.CompetitionVersions.AsNoTracking()
            .AsSplitQuery()
            .Include(version => version.Tasks)
            .ThenInclude(task => task.Tests)
            .SingleOrDefaultAsync(
                version => version.CompetitionId == id &&
                           version.VersionNumber == competition.Version,
                cancellationToken);
        if (currentVersion is null)
            return Conflict("The current competition version was not found.");

        var name = request.Name?.Trim() ?? currentVersion.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(request.Name), "Name cannot be empty.");
            return ValidationProblem(ModelState);
        }

        var description = request.Description is null
            ? currentVersion.Description
            : string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();
        var startTime = request.StartTime ?? currentVersion.StartTime;
        var endTime = request.EndTime ?? currentVersion.EndTime;

        if (startTime is not null && endTime is not null && endTime <= startTime)
        {
            ModelState.AddModelError(nameof(request.EndTime), "EndTime must be later than StartTime.");
            return ValidationProblem(ModelState);
        }

        var newVersion = new CompetitionVersion
        {
            CompetitionId = competition.Id,
            VersionNumber = competition.Version + 1,
            Name = name,
            Description = description,
            StartTime = startTime,
            EndTime = endTime,
            AllowViewOthersCode = request.AllowViewOthersCode ?? currentVersion.AllowViewOthersCode
        };

        foreach (var oldTask in currentVersion.Tasks.OrderBy(task => task.OrderNo))
        {
            var newTask = new CompetitionTask
            {
                CompetitionVersion = newVersion,
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

            newVersion.Tasks.Add(newTask);
        }

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

        return Ok(ToResponse(competition, newVersion));
    }

    [HttpPatch("{id}/archive")]
    public async Task<ActionResult<CompetitionArchiveResponse>> SetArchived(
        [FromRoute] string id,
        [FromBody] SetCompetitionArchivedRequest request,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (request.Archived is null)
            return BadRequest("Archived must be provided.");

        var competition = await db.Competitions.SingleOrDefaultAsync(
            item => item.Id == id && item.CreatorId == userId,
            cancellationToken);
        if (competition is null)
            return NotFound();

        if (request.Archived.Value)
        {
            if (competition.Status == CompetitionStatus.Draft)
                return BadRequest("A draft competition cannot be archived.");

            competition.Status = CompetitionStatus.Archived;
        }
        else if (competition.Status == CompetitionStatus.Archived)
        {
            competition.Status = CompetitionStatus.Published;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new CompetitionArchiveResponse(
            competition.Id,
            competition.Status,
            competition.Version));
    }

    private static CompetitionEditResponse ToResponse(
        Competition competition,
        CompetitionVersion version) =>
        new(
            competition.Id,
            version.Name,
            version.Description,
            competition.CreatorId,
            competition.Status,
            version.VersionNumber,
            version.StartTime,
            version.EndTime,
            version.AllowViewOthersCode,
            version.CreatedAt);

    public sealed record UpdateCompetitionRequest(
        [StringLength(200, MinimumLength = 1)] string? Name = null,
        string? Description = null,
        DateTimeOffset? StartTime = null,
        DateTimeOffset? EndTime = null,
        bool? AllowViewOthersCode = null);

    public sealed record SetCompetitionArchivedRequest(
        [Required] bool? Archived);

    public sealed record CompetitionEditResponse(
        string Id,
        string Name,
        string? Description,
        string CreatorId,
        CompetitionStatus Status,
        int Version,
        DateTimeOffset? StartTime,
        DateTimeOffset? EndTime,
        bool AllowViewOthersCode,
        DateTimeOffset VersionCreatedAt);

    public sealed record CompetitionArchiveResponse(
        string Id,
        CompetitionStatus Status,
        int Version);
}
