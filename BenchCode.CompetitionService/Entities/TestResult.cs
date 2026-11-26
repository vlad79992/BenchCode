using BenchCode.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace BenchCode.CompetitionService.Entities;

public class TestResult
{
    [Key]
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public int TestId { get; set; }
    public Verdict Verdict { get; set; }
    public int TimeMs { get; set; }
    public int MemoryKb { get; set; }
    public string? Diff { get; set; }
    public Submission? Submission { get; set; }
    public CompetitionTest? Test { get; set; }
}
