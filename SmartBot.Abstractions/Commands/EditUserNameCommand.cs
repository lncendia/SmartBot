using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для изменения ФИО пользователя
/// </summary>
public class EditUserNameCommand : TelegramCommand
{
    /// <summary>
    /// ФИО пользователя
    /// </summary>
    public string? FullName { get; init; }
}