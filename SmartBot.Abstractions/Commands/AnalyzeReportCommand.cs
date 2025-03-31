using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;
    
/// <summary>
/// Команда для анализа отчета
/// </summary>
public class AnalyzeReportCommand : TelegramCommand
{
    /// <summary>
    /// Отчет
    /// </summary>
    public string? Report { get; init; }
    
    /// <summary>
    /// Идентификатор сообщения
    /// </summary>
    public required int MessageId { get; init; }
}