namespace SmartBot.Abstractions.Models.Users;

/// <summary>
/// Класс, содержащий информацию о проверяемом отчёте
/// </summary>
/// <remarks>
/// Используется для хранения контекста проверки отчётов администраторами.
/// Связывает отчёт с процессом модерации и оставления комментариев.
/// </remarks>
public class ReviewingReport
{
    /// <summary>
    /// Уникальный идентификатор проверяемого отчёта
    /// </summary>
    /// <value>
    /// GUID, однозначно идентифицирующий отчёт в системе.
    /// Используется для поиска и привязки комментариев.
    /// </value>
    public required Guid ReportId { get; init; }
    
    /// <summary>
    /// Флаг, указывающий тип проверяемого отчёта
    /// </summary>
    /// <value>
    /// true - вечерний отчёт
    /// false - утренний отчёт
    /// </value>
    public required bool EveningReport { get; init; }
}