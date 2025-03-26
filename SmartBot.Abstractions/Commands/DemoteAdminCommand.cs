using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для удаления пользователя из числа администраторов.
/// </summary>
public class DemoteAdminCommand : TelegramCommand
{
    /// <summary>
    /// ID пользователя, которого нужно удалить из числа администраторов.
    /// </summary>
    public required long AdminId { get; init; }
}