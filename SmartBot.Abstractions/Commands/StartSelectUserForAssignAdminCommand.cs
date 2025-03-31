using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для начала процесса выбора пользователя для назначения его администратором.
/// </summary>
public class StartSelectUserForAssignAdminCommand : CallbackQueryCommand
{
    /// <summary>
    /// Флаг, указывающий, должен ли пользователь быть назначен как "Теле-администратор".
    /// </summary>
    public required bool TeleAdmin { get; init; }
}