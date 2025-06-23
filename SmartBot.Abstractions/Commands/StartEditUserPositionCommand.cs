using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для начала процесса изменения должности пользователя.
/// </summary>
public class StartEditUserPositionCommand : TelegramCommand
{
    /// <summary>
    /// ID пользователя, должности которого нужно изменить.
    /// </summary>
    public required long UserId { get; init; }
}