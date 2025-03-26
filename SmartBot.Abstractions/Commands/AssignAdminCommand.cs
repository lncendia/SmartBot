using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для добавления нового администратора.
/// </summary>
public class AssignAdminCommand : TelegramCommand
{
    /// <summary>
    /// ID пользователя, которого нужно назначить администратором.
    /// </summary>
    public required long AdminId { get; init; }
    
    /// <summary>
    /// Флаг, указывающий, должен ли пользователь быть назначен как "Теле-администратор".
    /// </summary>
    public required bool TeleAdmin { get; init; }
}