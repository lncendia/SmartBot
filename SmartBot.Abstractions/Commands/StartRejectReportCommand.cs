using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для начала отклонения отчёта
/// </summary>
public class StartRejectReportCommand : CallbackQueryCommand
{
    /// <summary>
    /// Идентификатор отчёта
    /// </summary>
    public required Guid ReportId { get; init; }
    
    /// <summary>
    /// Флаг, указывающий на тип отчета (утренний/вечерний)
    /// </summary>
    /// <value>
    /// true - вечерний отчет, false - утренний отчет
    /// </value>
    public required bool EveningReport { get; init; }
}