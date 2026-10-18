namespace BenchCodeMainService.Entities;

public class Submission
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid TaskId { get; init; }
    public required string UserName { get; init; }
    public required Language Language { get; init; }
    public required string Code { get; init; }
    public required DateTimeOffset SubmittedAt { get; init; }
}
