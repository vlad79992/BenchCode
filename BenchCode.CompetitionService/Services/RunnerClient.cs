using System.Net.Http.Json;
using CompetitionLanguage = BenchCode.CompetitionService.Entities.Language;
using BenchCode.Shared.Entities;

namespace BenchCode.CompetitionService.Services;

public sealed class RunnerClient(HttpClient httpClient) : ISubmissionRunner
{
    public async Task<SubmissionResult> RunAsync(
        CompetitionLanguage language,
        string code,
        IReadOnlyList<SubmissionTestCase> tests,
        CancellationToken cancellationToken)
    {
        var request = new RunnerRequest(
            language.ToString().ToLowerInvariant(),
            code,
            RunsPerTest: 1,
            tests.Select(test => new RunnerTestRequest(test.Input, test.Expected)).ToArray());

        using var response = await httpClient.PostAsJsonAsync(
            "Runner",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SubmissionResult>(
                   cancellationToken: cancellationToken)
               ?? throw new InvalidOperationException("ProgramRunner returned an empty response.");
    }

    private sealed record RunnerRequest(
        string Language,
        string Code,
        int RunsPerTest,
        IReadOnlyList<RunnerTestRequest> Tests);

    private sealed record RunnerTestRequest(string Input, string Expected);
}
