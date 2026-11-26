namespace BenchCode.Shared.Entities;

public sealed record SubmissionResult(
    bool Built,
    string? BuildError,
    IReadOnlyList<RunOutcome> Runs)
{
    public Verdict Final => !Built
        ? Verdict.CE
        : Runs.FirstOrDefault(r => r.Verdict != Verdict.OK)?.Verdict ?? Verdict.OK;
}