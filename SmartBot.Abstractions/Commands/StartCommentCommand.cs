namespace SmartBot.Abstractions.Commands;
    
/// <summary>
/// Команда для начала комментирования отчёта
/// </summary>
public class StartCommentCommand : TelegramCommand
{
    /// <summary>
    /// Идентификатор отчёта
    /// </summary>
    public required Guid ReportId { get; init; }
    
    /// <summary>
    /// Идентификатор сообщения
    /// </summary>
    public required int MessageId {get; init; }
}