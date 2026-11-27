using BenchCode.CompetitionService.Entities;
using BenchCode.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenchCode.CompetitionService.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/me/competitions")]
public sealed class MyCompetitionsController(CompetitionDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompetitionListItem>>> GetMyCompetitions(
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        [FromQuery] string? scope,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var competitions = db.Competitions.AsNoTracking();
        switch (scope?.Trim().ToLowerInvariant())
        {
            case "joined":
                competitions = competitions.Where(competition =>
                    competition.Participations.Any(participation =>
                        participation.UserId == userId &&
                        participation.Permission == ContestPermission.Participant));
                break;
            case "owned":
                competitions = competitions.Where(competition =>
                    competition.CreatorId == userId &&
                    competition.Status != CompetitionStatus.Archived);
                break;
            case "archived":
                competitions = competitions.Where(competition =>
                    competition.CreatorId == userId &&
                    competition.Status == CompetitionStatus.Archived);
                break;
            default:
                return BadRequest("Scope must be 'joined', 'owned', or 'archived'.");
        }

        var items = await (
            from competition in competitions
            join version in db.CompetitionVersions.AsNoTracking()
                on new { CompetitionId = competition.Id, VersionNumber = competition.Version }
                equals new { version.CompetitionId, version.VersionNumber }
            orderby version.Name
            select new CompetitionListItem(
                competition.Id,
                version.Name,
                version.Description,
                competition.Status,
                competition.Version,
                version.StartTime,
                version.EndTime))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    public sealed record CompetitionListItem(
        string Id,
        string Name,
        string? Description,
        CompetitionStatus Status,
        int Version,
        DateTimeOffset? StartTime,
        DateTimeOffset? EndTime);
}
