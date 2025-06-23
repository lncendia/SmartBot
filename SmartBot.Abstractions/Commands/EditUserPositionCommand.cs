using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для изменения должности пользователя
/// </summary>
public class EditUserPositionCommand : TelegramCommand
{
    /// <summary>
    /// Должность пользователя
    /// </summary>
    public string? Position { get; init; }
}