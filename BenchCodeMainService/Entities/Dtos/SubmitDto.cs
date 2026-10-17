using BenchCodeMainService.Entities;

namespace BenchCodeMainService.Entities.Dtos;

public class SubmitDto
{
    public int UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public Languages Language { get; set; }
}