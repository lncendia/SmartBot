using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для начала комментирования отчёта
/// </summary>
public class StartCommentCommand : CallbackQueryCommand
{
    /// <summary>
    /// Идентификатор отчёта
    /// </summary>
    public required Guid ReportId { get; init; }
    
    /// <summary>
    /// 
    /// </summary>
    public required bool EveningReport { get; init; }
}