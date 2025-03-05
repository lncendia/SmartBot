namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для возврата в состояние Idle (отмена текущего действия).
/// </summary>
public class GoBackCommand : TelegramCommand
{
    /// <summary>
    /// ID сообщения, которое нужно удалить.
    /// </summary>
    public required int MessageId { get; init; }
}