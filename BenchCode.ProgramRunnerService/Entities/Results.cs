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
    TimeSpan Time,
    IReadOnlyList<TimeSpan> RunTimes)
{
    public static RunOutcome Aggregate(IReadOnlyCollection<RunOutcome> executions)
    {
        ArgumentNullException.ThrowIfNull(executions);
        if (executions.Count == 0)
            throw new ArgumentException("At least one execution is required.", nameof(executions));

        var runs = executions.ToArray();
        var orderedTimes = runs.Select(run => run.Time).OrderBy(time => time).ToArray();
        var middle = orderedTimes.Length / 2;
        var median = orderedTimes.Length % 2 == 1
            ? orderedTimes[middle]
            : TimeSpan.FromTicks(
                orderedTimes[middle - 1].Ticks +
                (orderedTimes[middle].Ticks - orderedTimes[middle - 1].Ticks) / 2);

        var representative = runs.FirstOrDefault(run => run.Verdict != Verdict.AC) ?? runs[0];
        return representative with
        {
            Time = median,
            RunTimes = runs.Select(run => run.Time).ToArray(),
        };
    }
}

public sealed record SubmissionResult(bool Built, string? BuildError, IReadOnlyList<RunOutcome> Runs)
{
    public Verdict Final => !Built
        ? Verdict.CE
        : Runs.FirstOrDefault(r => r.Verdict != Verdict.AC)?.Verdict ?? Verdict.AC;
}