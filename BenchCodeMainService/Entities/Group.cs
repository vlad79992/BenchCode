namespace BenchCodeMainService.Entities;

public class Group
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public List<int> StudentIds { get; init; } = [];
}
