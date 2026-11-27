using System.ComponentModel.DataAnnotations;

namespace BenchCode.CompetitionService.Entities;

/// <summary>
/// Класс для хранения соревнования
/// </summary>
public class Competition
{
    /// <summary>
    /// Уникальный идентификатор соревнования. <br/>
    /// Состоит из 6 символов.
    /// Каждый символ может быть цифрой или буквой латинского алфавита в верхнем регистре. <br/>
    /// Исключены символы, которые могут быть спутаны с другими: <c>0</c>, <c>O</c>, <c>I</c>, <c>1</c>
    /// </summary>
    [Key]
    [Length(minimumLength: 6, maximumLength: 6)]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string CreatorId { get; set; } = string.Empty;

    /// <summary>
    /// Статус соревнования.
    /// Может быть:
    /// <list type="bullet">
    ///   <item>
    ///     <description><see cref="CompetitionStatus.Draft"/> - черновик.</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="CompetitionStatus.Published"/> — опубликовано.</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="CompetitionStatus.Archived"/> — архивировано.</description>
    ///   </item>
    /// </list>
    /// </summary>
    public CompetitionStatus Status { get; set; } = CompetitionStatus.Draft;

    /// <summary>
    /// Версия текущего состояния. При изменении содержимого предыдущее состояние
    /// сохраняется в истории, затем номер увеличивается.
    /// </summary>
    [ConcurrencyCheck]
    public int Version { get; set; } = 1;

    public ICollection<Participation> Participations { get; set; } = [];
    public ICollection<CompetitionVersion> Versions { get; set; } = [];

    /// <summary>
    /// Функция для генерации псевдослучайного кода соревнования
    /// </summary>
    /// <returns></returns>
    public static string GenerateId()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Repeat(chars, 6)
                                    .Select(s => s[Random.Shared.Next(s.Length)])
                                    .ToArray());
    }
}

public enum CompetitionStatus
{
    Draft,
    Published,
    Archived
}