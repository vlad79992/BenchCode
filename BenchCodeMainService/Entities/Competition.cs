namespace BenchCodeMainService.Entities;
public class Competition
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required DateTimeOffset StartDate { get; init; }
    public required DateTimeOffset EndDate { get; init; }
    public required List<Task> Tasks { get; init; }
    public required string Password { get; init; }
}
