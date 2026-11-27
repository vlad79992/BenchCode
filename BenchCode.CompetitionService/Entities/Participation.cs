using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BenchCode.CompetitionService.Entities;

[Index(nameof(CompetitionId), nameof(UserId), IsUnique = true)]
public class Participation
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [Required]
    public string CompetitionId { get; set; } = string.Empty;
    [ForeignKey(nameof(CompetitionId))]
    public Competition? Competition { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ContestPermission Permission { get; set; } = ContestPermission.Participant;

    /// <summary>
    /// Скрывает участника из чужих решений и рейтинга; владелец соревнования видит его.
    /// </summary>
    public bool IsHidden { get; set; }
}

public enum ContestPermission
{
    Owner,
    Participant
}
