using SmartBot.Abstractions.Attributes;
using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для отправки отчёта на ручную проверку.
/// </summary>
[AsyncCommand]
public class ManualReportAnalysisCommand : CallbackQueryCommand
{
    /// <summary>
    /// Идентификатор сообщения с отчётом.
    /// </summary>
    public required int ReportMessageId { get; init; }
}