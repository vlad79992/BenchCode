using System.ComponentModel.DataAnnotations;
using BenchCode.ProgramRunnerService.Entities;
using BenchCode.ProgramRunnerService.Entities.LanguageProfiles;
using BenchCode.Shared.Entities;
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
            "java" => new JavaProfile(),
            "csharp" => new CSharpProfile(),
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
            var outcome = await programRunner.RunAsync(profile,
                                                       artifact,
                                                       new TestCase(test!.Input!, test.Expected!),
                                                       cancellationToken);
            if (outcome.Verdict != Verdict.OK)
            {
                runs.Add(outcome);
                break;
            }
            var (totalTicks, totalRuns) = (outcome.Time.Ticks, 1);
            for (var i = 0; i < request.RunsPerTest - 1; i++)
            {
                var tempOutcome = await programRunner.RunAsync(profile,
                                                               artifact,
                                                               new TestCase(test!.Input!, test.Expected!),
                                                               cancellationToken);
                (totalTicks, totalRuns) = (totalTicks + tempOutcome.Time.Ticks, totalRuns + 1);

                if (tempOutcome.Verdict != Verdict.OK)
                {
                    outcome = tempOutcome;
                    break;
                }
            }
            runs.Add(outcome with { Time = TimeSpan.FromTicks(totalTicks / totalRuns)});
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

    [Range(1, 5)]
    public int RunsPerTest { get; init; } = 1;

    [Required, MinLength(1)]
    public List<RunnerTestRequest?> Tests { get; init; } = [];
}

public sealed record RunnerTestRequest
{
    public string? Input { get; init; } = string.Empty;

    public string? Expected { get; init; } = string.Empty;
}
