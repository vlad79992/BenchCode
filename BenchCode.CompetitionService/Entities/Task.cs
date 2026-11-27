using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BenchCode.CompetitionService.Entities;

[Index(nameof(CompetitionVersionId), nameof(OrderNo), IsUnique = true)]
public class Task
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid CompetitionVersionId { get; set; }
    [ForeignKey(nameof(CompetitionVersionId))]
    public CompetitionVersion? CompetitionVersion { get; set; }

    /// <summary>
    /// Номер задачи для отображения в определенном порядке
    /// </summary>
    public int OrderNo { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Описание задачи в формате Markdown
    /// </summary>
    public string? Description { get; set; }

    public int TimeLimitMs { get; set; } = 1000;
    public int MemoryLimitMb { get; set; } = 256;

    public bool IsArchived { get; set; }


    public ICollection<Test> Tests { get; set; } = [];

    [DeleteBehavior(DeleteBehavior.Restrict)]
    public ICollection<Submission> Submissions { get; set; } = [];
}
