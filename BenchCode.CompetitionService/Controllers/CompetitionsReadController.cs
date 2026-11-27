using BenchCode.CompetitionService.Entities;
using BenchCode.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenchCode.CompetitionService.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/competitions")]
public sealed class CompetitionsReadController(CompetitionDbContext db) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<CompetitionDetails>> GetById(
        [FromRoute] string id,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var competition = await db.Competitions.AsNoTracking()
            .Where(item => item.Id == id)
            .Join(
                db.CompetitionVersions.AsNoTracking(),
                item => new { CompetitionId = item.Id, VersionNumber = item.Version },
                version => new { version.CompetitionId, version.VersionNumber },
                (item, version) => new CompetitionDetails(
                    item.Id,
                    version.Name,
                    version.Description,
                    item.CreatorId,
                    item.Status,
                    item.Version,
                    version.StartTime,
                    version.EndTime,
                    version.AllowViewOthersCode,
                    version.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return competition is null ? NotFound() : Ok(competition);
    }

    [HttpGet("{id}/versions")]
    public async Task<ActionResult<IReadOnlyList<CompetitionVersionListItem>>> GetVersions(
        [FromRoute] string id,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var isOwner = await db.Competitions.AsNoTracking()
            .AnyAsync(competition => competition.Id == id && competition.CreatorId == userId,
                      cancellationToken);
        if (!isOwner)
            return NotFound();

        var versions = await db.CompetitionVersions.AsNoTracking()
            .Where(version => version.CompetitionId == id)
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => new CompetitionVersionListItem(
                version.VersionNumber,
                version.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(versions);
    }

    [HttpGet("{id}/versions/{versionNumber:int}")]
    public async Task<ActionResult<CompetitionVersionDetails>> GetVersionByNumber(
        [FromRoute] string id,
        [FromRoute] int versionNumber,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var version = await db.CompetitionVersions.AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Tasks)
            .ThenInclude(task => task.Tests)
            .FirstOrDefaultAsync(item =>
                item.CompetitionId == id &&
                item.VersionNumber == versionNumber &&
                item.Competition != null &&
                item.Competition.CreatorId == userId,
                cancellationToken);
        if (version is null)
            return NotFound();

        var result = new CompetitionVersionDetails(
            version.CompetitionId,
            version.VersionNumber,
            version.Name,
            version.Description,
            version.StartTime,
            version.EndTime,
            version.AllowViewOthersCode,
            version.CreatedAt,
            version.Tasks
                .OrderBy(task => task.OrderNo)
                .Select(task => new CompetitionTaskVersionDetails(
                    task.Id,
                    task.OrderNo,
                    task.Name,
                    task.Description,
                    task.TimeLimitMs,
                    task.MemoryLimitMb,
                    task.IsArchived,
                    task.Tests
                        .OrderBy(test => test.OrderNo)
                        .Select(test => new CompetitionTestVersionDetails(
                            test.Id,
                            test.OrderNo,
                            test.IsPublic,
                            test.Input,
                            test.Expected))
                        .ToArray()))
                .ToArray());

        return Ok(result);
    }

    public sealed record CompetitionDetails(
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

    public sealed record CompetitionVersionListItem(
        int Version,
        DateTimeOffset CreatedAt);

    public sealed record CompetitionVersionDetails(
        string CompetitionId,
        int Version,
        string Name,
        string? Description,
        DateTimeOffset? StartTime,
        DateTimeOffset? EndTime,
        bool AllowViewOthersCode,
        DateTimeOffset CreatedAt,
        IReadOnlyList<CompetitionTaskVersionDetails> Tasks);

    public sealed record CompetitionTaskVersionDetails(
        Guid Id,
        int OrderNo,
        string Name,
        string? Description,
        int TimeLimitMs,
        int MemoryLimitMb,
        bool IsArchived,
        IReadOnlyList<CompetitionTestVersionDetails> Tests);

    public sealed record CompetitionTestVersionDetails(
        Guid Id,
        int OrderNo,
        bool IsPublic,
        string Input,
        string Expected);
}
