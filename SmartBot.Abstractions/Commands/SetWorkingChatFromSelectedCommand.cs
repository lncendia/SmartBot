using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для установки рабочего чата пользователю.
/// </summary>
public class SetWorkingChatFromSelectedCommand : TelegramCommand
{
    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public required long UserId { get; init; }
}