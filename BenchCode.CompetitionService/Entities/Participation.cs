using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BenchCode.CompetitionService.Entities;

public class Participation
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string CompetitionId { get; set; } = string.Empty;
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ContestPermission Permission { get; set; } = ContestPermission.Participant;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    [ForeignKey(nameof(CompetitionId))]
    public Competition? Competition { get; set; }
}

public enum ContestPermission
{
    Owner,
    Participant
}
