using System.ComponentModel.DataAnnotations;
using BenchCode.CompetitionService.Entities;
using BenchCode.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenchCode.CompetitionService.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/competitions/{competitionId}")]
public sealed class CompetitionParticipantsController(CompetitionDbContext db) : ControllerBase
{
    [HttpPost("join")]
    public async Task<ActionResult<ParticipantResponse>> Join(
        [FromRoute] string competitionId,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var competition = await db.Competitions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == competitionId, cancellationToken);
        if (competition is null)
            return NotFound();

        if (competition.Status != CompetitionStatus.Published)
            return BadRequest("Only published competitions can be joined.");

        var alreadyParticipates = await db.Participations.AsNoTracking()
            .AnyAsync(item => item.CompetitionId == competitionId && item.UserId == userId,
                      cancellationToken);
        if (alreadyParticipates)
            return Conflict("The user already participates in this competition.");

        var participation = new Participation
        {
            CompetitionId = competitionId,
            UserId = userId,
            Permission = ContestPermission.Participant
        };
        db.Participations.Add(participation);
        await db.SaveChangesAsync(cancellationToken);

        var response = new ParticipantResponse(
            participation.UserId,
            participation.Permission,
            participation.IsHidden);

        return Created($"/api/competitions/{competitionId}/participants", response);
    }

    [HttpGet("participants")]
    public async Task<ActionResult<IReadOnlyList<ParticipantResponse>>> GetParticipants(
        [FromRoute] string competitionId,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var competition = await db.Competitions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == competitionId, cancellationToken);
        if (competition is null)
            return NotFound();

        var isOwner = competition.CreatorId == userId;
        if (!isOwner)
        {
            var isParticipant = await db.Participations.AsNoTracking()
                .AnyAsync(item => item.CompetitionId == competitionId && item.UserId == userId,
                          cancellationToken);
            if (!isParticipant)
                return NotFound();
        }

        var participants = db.Participations.AsNoTracking()
            .Where(item => item.CompetitionId == competitionId);
        if (!isOwner)
        {
            participants = participants.Where(item => !item.IsHidden || item.UserId == userId);
        }

        var result = await participants
            .OrderBy(item => item.UserId)
            .Select(item => new ParticipantResponse(
                item.UserId,
                item.Permission,
                item.IsHidden))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPatch("participants/{participantUserId}/visibility")]
    public async Task<ActionResult<ParticipantResponse>> SetVisibility(
        [FromRoute] string competitionId,
        [FromRoute] string participantUserId,
        [FromBody] SetParticipantVisibilityRequest request,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var competition = await db.Competitions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == competitionId && item.CreatorId == userId,
                                  cancellationToken);
        if (competition is null)
            return NotFound();

        var participation = await db.Participations.SingleOrDefaultAsync(
            item => item.CompetitionId == competitionId && item.UserId == participantUserId,
            cancellationToken);
        if (participation is null)
            return NotFound();

        if (participation.Permission == ContestPermission.Owner ||
            participantUserId == competition.CreatorId)
        {
            return BadRequest("The competition owner cannot be hidden.");
        }

        participation.IsHidden = request.IsHidden!.Value;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new ParticipantResponse(
            participation.UserId,
            participation.Permission,
            participation.IsHidden));
    }

    public sealed record SetParticipantVisibilityRequest([Required] bool? IsHidden);

    public sealed record ParticipantResponse(
        string UserId,
        ContestPermission Permission,
        bool IsHidden);
}
