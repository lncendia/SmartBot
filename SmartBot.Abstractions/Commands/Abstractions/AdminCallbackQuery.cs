namespace SmartBot.Abstractions.Commands.Abstractions;

/// <summary>
/// 
/// </summary>
public abstract class AdminCallbackQuery : TelegramCommand
{
    /// <summary>
    /// Идентификатор нажатия кнопки.
    /// </summary>
    public required string CallbackQueryId { get; init; }

    /// <summary>
    /// Идентификатор сообщения.
    /// </summary>
    public required int MessageId { get; init; }
}