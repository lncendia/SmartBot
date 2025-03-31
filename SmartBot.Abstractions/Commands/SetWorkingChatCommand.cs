using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для установки рабочего чата пользователю.
/// </summary>
public class SetWorkingChatCommand : CallbackQueryCommand
{
    /// <summary>
    /// Идентификатор рабочего чата.
    /// </summary>
    public required long WorkingChatId { get; init; }
    
    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public required long UserId { get; init; }
}