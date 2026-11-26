using System.ComponentModel.DataAnnotations;

namespace BenchCode.CompetitionService.Entities;

public class CompetitionTask
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string CompetitionId { get; set; } = string.Empty;
    public int OrderNo { get; set; }
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public string StatementMarkdown { get; set; } = string.Empty;
    public int TimeLimitMs { get; set; } = 1000;
    public int MemoryLimitMb { get; set; } = 256;
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Competition? Competition { get; set; }
    public ICollection<CompetitionTest> Tests { get; set; } = new List<CompetitionTest>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
