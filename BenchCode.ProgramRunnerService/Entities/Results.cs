using BenchCode.Shared.Entities;

namespace BenchCode.ProgramRunnerService.Entities;

public sealed record Artifact(byte[] Bytes);

public sealed record BuildOutcome(Artifact? Artifact, string? Error)
{
    public bool Success => Artifact is not null;
    public static BuildOutcome Ok(Artifact artifact) => new(artifact, null);
    public static BuildOutcome Fail(string error) => new(null, error);
}