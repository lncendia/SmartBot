using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для установки ФИО пользователя
/// </summary>
public class SetFullNameCommand : TelegramCommand
{
    /// <summary>
    /// ФИО пользователя
    /// </summary>
    public string? FullName { get; init; }
}