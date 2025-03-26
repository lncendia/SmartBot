using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для установки должности пользователя
/// </summary>
public class SetPositionCommand : TelegramCommand
{
    /// <summary>
    /// Должность пользователя
    /// </summary>
    public string? Position { get; init; }
}