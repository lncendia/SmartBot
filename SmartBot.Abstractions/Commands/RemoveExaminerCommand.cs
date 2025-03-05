namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для удаления пользователя из числа проверяющих.
/// </summary>
public class RemoveExaminerCommand : TelegramCommand
{
    /// <summary>
    /// ID пользователя, которого нужно удалить из числа проверяющих.
    /// </summary>
    public string? ExaminerId { get; init; }
}