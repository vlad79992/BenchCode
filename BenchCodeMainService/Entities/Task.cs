namespace BenchCodeMainService.Entities;

public class Task
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<Languages> Languages { get; init; } = [];
    public List<Submit> Submits { get; init; } = [];
}