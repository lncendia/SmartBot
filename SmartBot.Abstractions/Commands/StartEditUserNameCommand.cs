using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для начала процесса изменения имени пользователя.
/// </summary>
public class StartEditUserNameCommand : TelegramCommand
{
    /// <summary>
    /// ID пользователя, имя которого нужно изменить.
    /// </summary>
    public required long UserId { get; init; }
}