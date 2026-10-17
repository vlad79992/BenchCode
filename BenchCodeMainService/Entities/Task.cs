namespace BenchCodeMainService.Entities;

public class Task
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required List<Language> Languages { get; init; }
    public List<Submission> Submissions { get; init; } = [];
}
