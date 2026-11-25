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
        var profile = ResolveProfile(request.Language);

        if (profile is null)
            return BadRequest($"Unsupported language '{request.Language}'.");

        if (request.Tests.Any(test => test is null || test.Input is null || test.Expected is null))
            return BadRequest("Each test must include input and expected output.");

        var hasCheckerCode = request.CheckerCode is not null;
        var hasCheckerLanguage = !string.IsNullOrWhiteSpace(request.CheckerLanguage);
        if (hasCheckerCode != hasCheckerLanguage ||
            (hasCheckerCode && string.IsNullOrWhiteSpace(request.CheckerCode)))
        {
            return BadRequest("Custom checker code and language must both be provided.");
        }

        LanguageProfile? checkerProfile = null;
        Artifact? checkerArtifact = null;
        if (hasCheckerCode)
        {
            checkerProfile = ResolveProfile(request.CheckerLanguage!);
            if (checkerProfile is null)
                return BadRequest($"Unsupported checker language '{request.CheckerLanguage}'.");

            var checkerBuild = await programRunner.BuildAsync(
                checkerProfile,
                request.CheckerCode!,
                cancellationToken);
            if (!checkerBuild.Success)
                return BadRequest($"Custom checker compilation failed: {checkerBuild.Error}");

            checkerArtifact = checkerBuild.Artifact!;
        }

        var build = await programRunner.BuildAsync(profile, request.Code, cancellationToken);
        if (!build.Success)
            return Ok(new SubmissionResult(false, build.Error, []));

        var artifact = build.Artifact!;
        var runs = new List<RunOutcome>(request.Tests.Count);
        foreach (var test in request.Tests)
        {
            var samples = new List<RunOutcome>(request.RunsPerTest);
            for (var i = 0; i < request.RunsPerTest; i++)
            {
                samples.Add(await programRunner.RunAsync(
                    profile,
                    artifact,
                    new TestCase(test!.Input!, test.Expected!),
                    cancellationToken,
                    checkerProfile,
                    checkerArtifact));
            }

            runs.Add(RunOutcome.Aggregate(samples));
        }

        return Ok(new SubmissionResult(true, null, runs));
    }

    private static LanguageProfile? ResolveProfile(string language) =>
        language.Trim().ToLowerInvariant() switch
        {
            "cpp" => new CppProfile(),
            "java" => new JavaProfile(),
            "csharp" => new CSharpProfile(),
            _ => null,
        };
}

public sealed record RunnerRequest
{
    [Required]
    public string Language { get; init; } = string.Empty;

    [Required]
    public string Code { get; init; } = string.Empty;

    [Range(1, 5)]
    public int RunsPerTest { get; init; } = 1;

    public string? CheckerLanguage { get; init; }

    public string? CheckerCode { get; init; }

    [Required, MinLength(1)]
    public List<RunnerTestRequest?> Tests { get; init; } = [];
}

public sealed record RunnerTestRequest
{
    public string? Input { get; init; } = string.Empty;

    public string? Expected { get; init; } = string.Empty;
}
