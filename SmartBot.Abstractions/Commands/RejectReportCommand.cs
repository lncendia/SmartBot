using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;
    
/// <summary>
/// Команда для отклонения отчёта.
/// </summary>
public class RejectReportCommand : TelegramCommand
{
    /// <summary>
    /// Текст с причиной отклонения.
    /// </summary>
    public string? Comment { get; init; }
}