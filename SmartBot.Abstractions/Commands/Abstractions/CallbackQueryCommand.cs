namespace SmartBot.Abstractions.Commands.Abstractions;

/// <summary>
/// Абстрактная команда CallbackQuery для Telegram
/// </summary>
public abstract class CallbackQueryCommand : TelegramCommand
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