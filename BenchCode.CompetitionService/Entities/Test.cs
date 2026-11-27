using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BenchCode.CompetitionService.Entities;

[Index(nameof(TaskId), nameof(OrderNo), IsUnique = true)]
public class Test
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid TaskId { get; set; }
    [ForeignKey(nameof(TaskId))]
    public Task? Task { get; set; }

    public int OrderNo { get; set; }

    public bool IsPublic { get; set; }

    [Required]
    public string Input { get; set; } = string.Empty;

    [Required]
    public string Expected { get; set; } = string.Empty;

}
