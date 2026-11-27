using BenchCode.Shared.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BenchCode.CompetitionService.Entities;

[Index(nameof(SubmissionId), nameof(TestId), IsUnique = true)]
public class Run
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid TestId { get; set; }
    [ForeignKey(nameof(TestId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public Test? Test { get; set; }

    public Guid SubmissionId { get; set; }
    [ForeignKey(nameof(SubmissionId))]
    public Submission? Submission { get; set; }

    public Verdict Verdict { get; set; }

    public string Stdout { get; set; } = string.Empty;

    public string Stderr { get; set; } = string.Empty;

    public long ExitCode { get; set; }

    public TimeSpan Time { get; set; }
}
