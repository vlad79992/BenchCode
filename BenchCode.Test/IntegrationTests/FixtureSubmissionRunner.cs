extern alias CompetitionService;
using BenchCode.Shared.Entities;
using CompetitionLanguage = CompetitionService::BenchCode.CompetitionService.Entities.Language;
using SubmissionTestCase = CompetitionService::BenchCode.CompetitionService.Services.SubmissionTestCase;
using ISubmissionRunner = CompetitionService::BenchCode.CompetitionService.Services.ISubmissionRunner;

namespace BenchCode.Test.IntegrationTests;

public sealed class FixtureSubmissionRunner : ISubmissionRunner
{
    public Task<SubmissionResult> RunAsync(
        CompetitionLanguage language,
        string code,
        IReadOnlyList<SubmissionTestCase> tests,
        CancellationToken cancellationToken) =>
        Task.FromResult(new SubmissionResult(
            Built: true,
            BuildError: null,
            Runs: tests.Select(_ => new RunOutcome(
                Verdict.OK,
                Stdout: "test output",
                Stderr: string.Empty,
                ExitCode: 0,
                TimeSpan.FromMilliseconds(5))).ToArray()));
}
