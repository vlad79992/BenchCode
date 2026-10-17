namespace BenchCodeMainService.Entities;

public class Submit
{
    public int Id { get; init; }
    public int TaskId { get; init; }
    public int UserId { get; init; }
    public string Code { get; init; } = string.Empty;
    public Languages Language { get; init; }
    public SubmitStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; init; }
}