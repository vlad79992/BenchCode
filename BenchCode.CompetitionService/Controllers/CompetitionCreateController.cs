using System.ComponentModel.DataAnnotations;
using BenchCode.CompetitionService.Entities;
using BenchCode.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenchCode.CompetitionService.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/competitions")]
public sealed class CompetitionCreateController(CompetitionDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CompetitionCreatedResponse>> Create(
        [FromBody] CreateCompetitionRequest request,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (request.StartTime is not null && request.EndTime is not null &&
            request.EndTime <= request.StartTime)
        {
            ModelState.AddModelError(
                nameof(request.EndTime),
                "EndTime must be later than StartTime.");
            return ValidationProblem(ModelState);
        }

        string id;
        do
        {
            id = Competition.GenerateId();
        }
        while (await db.Competitions.AnyAsync(competition => competition.Id == id, cancellationToken));

        var name = request.Name.Trim();
        var description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        var competition = new Competition
        {
            Id = id,
            CreatorId = userId,
            Status = CompetitionStatus.Draft,
            Version = 1
        };
        var version = new CompetitionVersion
        {
            CompetitionId = id,
            VersionNumber = 1,
            Name = name,
            Description = description,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            AllowViewOthersCode = request.AllowViewOthersCode
        };
        var ownerParticipation = new Participation
        {
            CompetitionId = id,
            UserId = userId,
            Permission = ContestPermission.Owner
        };

        db.AddRange(competition, version, ownerParticipation);
        await db.SaveChangesAsync(cancellationToken);

        var response = new CompetitionCreatedResponse(
            id,
            name,
            description,
            userId,
            competition.Status,
            competition.Version,
            version.StartTime,
            version.EndTime,
            version.AllowViewOthersCode,
            version.CreatedAt);

        return Created($"/api/competitions/{id}", response);
    }

    public sealed record CreateCompetitionRequest(
        [Required, StringLength(200)] string Name,
        string? Description = null,
        DateTimeOffset? StartTime = null,
        DateTimeOffset? EndTime = null,
        bool AllowViewOthersCode = false);

    public sealed record CompetitionCreatedResponse(
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
}
