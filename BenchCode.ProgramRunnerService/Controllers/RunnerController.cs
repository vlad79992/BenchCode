using System.ComponentModel.DataAnnotations;
using BenchCode.ProgramRunnerService.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BenchCode.ProgramRunnerService.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class RunnerController(ProgramRunner programRunner) : ControllerBase
{
    [HttpPost(Name = "RunCode")]
    public async Task<ActionResult<SubmissionResult>> RunCode(
        [FromBody] RunnerRequest request,
        CancellationToken cancellationToken)
    {
        LanguageProfile? profile = request.Language.Trim().ToLowerInvariant() switch
        {
            "cpp" => new CppProfile(),
            _ => null,
        };

        if (profile is null)
            return BadRequest($"Unsupported language '{request.Language}'.");

        if (request.Tests.Any(test => test is null || test.Input is null || test.Expected is null))
            return BadRequest("Each test must include input and expected output.");

        var build = await programRunner.BuildAsync(profile, request.Code, cancellationToken);
        if (!build.Success)
            return Ok(new SubmissionResult(false, build.Error, []));

        var artifact = build.Artifact!;
        var runs = new List<RunOutcome>(request.Tests.Count);
        foreach (var test in request.Tests)
        {
            runs.Add(await programRunner.RunAsync(
                profile,
                artifact,
                new TestCase(test!.Input!, test.Expected!),
                cancellationToken));
        }

        return Ok(new SubmissionResult(true, null, runs));
    }
}

public sealed record RunnerRequest
{
    [Required]
    public string Language { get; init; } = string.Empty;

    [Required]
    public string Code { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public List<RunnerTestRequest?> Tests { get; init; } = [];
}

public sealed record RunnerTestRequest
{
    public string? Input { get; init; } = string.Empty;

    public string? Expected { get; init; } = string.Empty;
}
