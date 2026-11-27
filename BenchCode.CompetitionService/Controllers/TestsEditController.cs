using System.ComponentModel.DataAnnotations;
using BenchCode.CompetitionService.Entities;
using BenchCode.CompetitionService.Services;
using BenchCode.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompetitionTest = BenchCode.CompetitionService.Entities.Test;

namespace BenchCode.CompetitionService.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal")]
public sealed class TestsEditController(CompetitionDbContext db) : ControllerBase
{
    [HttpPatch("tests/{testId}")]
    public async Task<ActionResult<TestEditResponse>> Edit(
        [FromRoute] Guid testId,
        [FromBody] UpdateTestRequest request,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        if (request.OrderNo is null && request.IsPublic is null &&
            request.Input is null && request.Expected is null)
        {
            return BadRequest("At least one test field must be provided.");
        }

        var location = await GetTestLocationAsync(testId, cancellationToken);
        if (location is null)
            return NotFound();

        var competition = await db.Competitions.SingleOrDefaultAsync(
            item => item.Id == location.CompetitionId && item.CreatorId == userId,
            cancellationToken);
        if (competition is null || competition.Version != location.VersionNumber)
            return NotFound();

        var currentVersion = await LoadVersionAsync(location.CompetitionVersionId, cancellationToken);
        if (currentVersion is null)
            return NotFound();

        var currentTask = currentVersion.Tasks.SingleOrDefault(task => task.Id == location.TaskId);
        var currentTest = currentTask?.Tests.SingleOrDefault(test => test.Id == testId);
        if (currentTest is null)
            return NotFound();

        var newOrderNo = request.OrderNo ?? currentTest.OrderNo;
        if (currentTask!.Tests.Any(test => test.Id != testId && test.OrderNo == newOrderNo))
            return Conflict("Another test already uses this order number.");

        var versionClone = CompetitionVersionCloner.Clone(
            currentVersion,
            competition.Version + 1);
        var newVersion = versionClone.Version;
        var updatedTest = versionClone.TestCopies[testId];

        if (request.OrderNo is not null)
            updatedTest.OrderNo = request.OrderNo.Value;
        if (request.IsPublic is not null)
            updatedTest.IsPublic = request.IsPublic.Value;
        if (request.Input is not null)
            updatedTest.Input = request.Input;
        if (request.Expected is not null)
            updatedTest.Expected = request.Expected;

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

        return Ok(ToResponse(updatedTest, location.CompetitionId, newVersion.VersionNumber));
    }

    [HttpDelete("tests/{testId}")]
    public async Task<ActionResult<CompetitionVersionChangedResponse>> Delete(
        [FromRoute] Guid testId,
        [FromHeader(Name = InternalRequestHeaders.UserId)] string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var location = await GetTestLocationAsync(testId, cancellationToken);
        if (location is null)
            return NotFound();

        var competition = await db.Competitions.SingleOrDefaultAsync(
            item => item.Id == location.CompetitionId && item.CreatorId == userId,
            cancellationToken);
        if (competition is null || competition.Version != location.VersionNumber)
            return NotFound();

        var currentVersion = await LoadVersionAsync(location.CompetitionVersionId, cancellationToken);
        if (currentVersion is null || currentVersion.Tasks
                .Where(task => task.Id == location.TaskId)
                .SelectMany(task => task.Tests)
                .All(test => test.Id != testId))
        {
            return NotFound();
        }

        var newVersion = CompetitionVersionCloner.Clone(
            currentVersion,
            competition.Version + 1,
            excludedTestId: testId).Version;

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

    private async Task<TestLocation?> GetTestLocationAsync(
        Guid testId,
        CancellationToken cancellationToken) =>
        await db.Tests.AsNoTracking()
            .Where(test => test.Id == testId)
            .Select(test => new TestLocation(
                test.TaskId,
                test.Task!.CompetitionVersionId,
                test.Task.CompetitionVersion!.CompetitionId,
                test.Task.CompetitionVersion.VersionNumber))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<CompetitionVersion?> LoadVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken) =>
        await db.CompetitionVersions.AsNoTracking()
            .AsSplitQuery()
            .Include(version => version.Tasks)
            .ThenInclude(task => task.Tests)
            .SingleOrDefaultAsync(version => version.Id == versionId, cancellationToken);

    private static TestEditResponse ToResponse(
        CompetitionTest test,
        string competitionId,
        int version) =>
        new(
            test.Id,
            competitionId,
            version,
            test.TaskId,
            test.OrderNo,
            test.IsPublic,
            test.Input,
            test.Expected);

    private sealed record TestLocation(
        Guid TaskId,
        Guid CompetitionVersionId,
        string CompetitionId,
        int VersionNumber);

    public sealed record UpdateTestRequest(
        [Range(0, int.MaxValue)] int? OrderNo = null,
        bool? IsPublic = null,
        string? Input = null,
        string? Expected = null);

    public sealed record TestEditResponse(
        Guid Id,
        string CompetitionId,
        int Version,
        Guid TaskId,
        int OrderNo,
        bool IsPublic,
        string Input,
        string Expected);

    public sealed record CompetitionVersionChangedResponse(
        string CompetitionId,
        int Version);
}
