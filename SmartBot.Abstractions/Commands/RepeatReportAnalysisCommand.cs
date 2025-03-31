using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для повторного анализа отчёта анализатором.
/// </summary>
public class RepeatReportAnalysisCommand : CallbackQueryCommand
{
    /// <summary>
    /// Идентификатор сообщения с отчётом.
    /// </summary>
    public required int ReportMessageId { get; init; }
}