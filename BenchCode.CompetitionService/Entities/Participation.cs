using System.ComponentModel.DataAnnotations;

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
    public Competition? Competition { get; set; }
}

public enum ContestPermission
{
    Owner,
    Participant
}
