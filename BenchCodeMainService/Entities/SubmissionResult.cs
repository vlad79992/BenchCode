namespace BenchCodeMainService.Entities;

public class SubmissionResult
{
    public required Guid SubmissionId { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
