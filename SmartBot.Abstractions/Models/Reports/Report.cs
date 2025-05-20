using SmartBot.Abstractions.Models.Users;

namespace SmartBot.Abstractions.Models.Reports;

/// <summary>
/// Класс, представляющий отчет.
/// </summary>
public class Report
{
    /// <summary>
    /// Уникальный идентификатор отчета.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор пользователя, связанного с отчетом.
    /// </summary>
    public required long UserId { get; init; }

    /// <summary>
    /// Навигационное свойство
    /// </summary>
    public User? User { get; init; }

    /// <summary>
    /// Утренний отчет.
    /// </summary>
    public required UserReport MorningReport { get; set; }

    /// <summary>
    /// Вечерний отчет.
    /// </summary>
    public UserReport? EveningReport { get; set; }

    /// <summary>
    /// Дата создания отчета.
    /// </summary>
    public required DateTime Date { get; init; }

    /// <summary>
    /// Комментарий, сгенерированный ботом.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="evening"></param>
    /// <returns></returns>
    public UserReport? GetReport(bool evening) => evening ? EveningReport : MorningReport;
}