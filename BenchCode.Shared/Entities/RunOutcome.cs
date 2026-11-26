namespace BenchCode.Shared.Entities;

public sealed record RunOutcome(
    Verdict Verdict,
    string Stdout,
    string Stderr,
    long ExitCode,
    TimeSpan Time);
