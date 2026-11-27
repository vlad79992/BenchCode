using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BenchCode.CompetitionService.Entities;

[Index(nameof(CompetitionId), nameof(VersionNumber), IsUnique = true)]
public class CompetitionVersion
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [Required, MaxLength(6)]
    public string CompetitionId { get; set; } = string.Empty;

    [ForeignKey(nameof(CompetitionId))]
    public Competition? Competition { get; set; }

    [Range(1, int.MaxValue)]
    public int VersionNumber { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Описание соревнования в формате Markdown.
    /// </summary>
    public string? Description { get; set; }

    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }

    public bool AllowViewOthersCode { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Task> Tasks { get; set; } = [];
}
