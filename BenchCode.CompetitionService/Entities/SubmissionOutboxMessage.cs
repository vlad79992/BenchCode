using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BenchCode.CompetitionService.Entities;

[Index(nameof(SubmissionId), IsUnique = true)]
[Index(nameof(PublishedAt), nameof(NextAttemptAt))]
public sealed class SubmissionOutboxMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid SubmissionId { get; set; }

    [ForeignKey(nameof(SubmissionId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Submission? Submission { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; }
    public int Attempts { get; set; }

    [MaxLength(2000)]
    public string? LastError { get; set; }
}
