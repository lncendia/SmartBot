namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для удаления пользователя.
/// </summary>
public class RemoveUserCommand : TelegramCommand
{
    /// <summary>
    /// ID пользователя, которого нужно удалить.
    /// </summary>
    public string? UserId { get; init; }
}