namespace BenchCode.ProgramRunnerService.Entities;

public enum Verdict
{
    AC  = 0,
    WA  = 1, // Wrong Answer
    TLE = 2, // Time Limit Exceeded
    MLE = 3, // Memory Limit Exceeded
    RE  = 4, // Runtime Error
    OLE = 5, // Output Limit Exceeded
    CE  = 6, // Compilation Error
}

public sealed record Artifact(byte[] Bytes);

public sealed record TestCase(string Input, string Expected);

public sealed record BuildOutcome(Artifact? Artifact, string? Error)
{
    public bool Success => Artifact is not null;
    public static BuildOutcome Ok(Artifact artifact) => new(artifact, null);
    public static BuildOutcome Fail(string error) => new(null, error);
}

public sealed record RunOutcome(
    Verdict Verdict,
    string Stdout,
    string Stderr,
    long ExitCode,
    TimeSpan Time);

public sealed record SubmissionResult(bool Built, string? BuildError, IReadOnlyList<RunOutcome> Runs)
{
    public Verdict Final => !Built
        ? Verdict.CE
        : Runs.FirstOrDefault(r => r.Verdict != Verdict.AC)?.Verdict ?? Verdict.AC;
}