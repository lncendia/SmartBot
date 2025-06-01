using SmartBot.Abstractions.Attributes;
using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;
    
/// <summary>
/// Команда для анализа отчета
/// </summary>
[AsyncCommand]
public class ApproveReportCommand : CallbackQueryCommand
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
    
    /// <summary>
    /// Имя пользователя Telegram
    /// </summary>
    public string? Username { get; init; }
    
    /// <summary>
    /// Имя пользователя автора отчёта Telegram
    /// </summary>
    public string? ReportUsername { get; init; }
}