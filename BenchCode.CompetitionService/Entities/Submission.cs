using BenchCode.Shared.Entities;
using System.ComponentModel.DataAnnotations;

namespace BenchCode.CompetitionService.Entities;

public class Submission
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string CompetitionId { get; set; } = string.Empty;
    public int TaskId { get; set; }
    [Required]
    public string UserId { get; set; } = string.Empty;
    public CompetitionLanguage Language { get; set; }
    [Required, MaxLength(65536)]
    public string Code { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public int CompetitionVersion { get; set; } = 1;
    public bool IsLate { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Testing;
    public Verdict? Verdict { get; set; }
    public int? TimeMs { get; set; }
    public int? MemoryKb { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CompilerOutput { get; set; }

    public CompetitionTask? Task { get; set; }
    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}

public enum SubmissionStatus
{
    Testing,
    Finished,
    SystemError
}

public enum CompetitionLanguage
{
    Cpp,
    CSharp,
    Java,
    C,
    Python
}