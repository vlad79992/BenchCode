namespace BenchCodeMainService.Entities;

public class Competition
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset EndDate { get; init; }
    public List<int> GroupIds { get; init; } = [];
    public List<Task> Tasks { get; init; } = [];
}
