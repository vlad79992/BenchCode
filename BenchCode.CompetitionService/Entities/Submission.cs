using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedSubmissionResult = BenchCode.Shared.Entities.SubmissionResult;

namespace BenchCode.CompetitionService.Entities;

public class Submission
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [Required]
    public string CompetitionId { get; set; } = string.Empty;
    [ForeignKey(nameof(CompetitionId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Competition? Competition { get; set; }

    public Guid TaskId { get; set; }
    [ForeignKey(nameof(TaskId))]
    public Task? Task { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public Language Language { get; set; }

    [Required, MaxLength(65536)]
    public string Code { get; set; } = string.Empty;

    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;

    public int CompetitionVersion { get; set; } = 1;

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Testing;

    public string? BuildError { get; set; }

    public Shared.Entities.Verdict? Verdict { get; set; }

    public ICollection<Run> Runs { get; set; } = [];
}

public enum SubmissionStatus
{
    Testing,
    Finished,
    SystemError
}

public enum Language
{
    Cpp,
    CSharp,
    Java,
    Python
}