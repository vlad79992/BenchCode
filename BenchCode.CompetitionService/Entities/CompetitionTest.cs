using System.ComponentModel.DataAnnotations;

namespace BenchCode.CompetitionService.Entities;

public class CompetitionTest
{
    [Key]
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int OrderNo { get; set; }
    public bool IsPublic { get; set; }
    public string? InputText { get; set; }
    public byte[]? InputFile { get; set; }
    public string? ExpectedOutputText { get; set; }
    public byte[]? ExpectedOutputFile { get; set; }
    public CompetitionTask? Task { get; set; }
}
