using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для удаления пользователя.
/// </summary>
public class BlockUserCommand : TelegramCommand
{
    /// <summary>
    /// ID пользователя, которого нужно удалить.
    /// </summary>
    public required long UserId { get; init; }
}