namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для добавления нового проверяющего.
/// </summary>
public class AddExaminerCommand  : TelegramCommand
{
    /// <summary>
    /// ID пользователя, которого нужно назначить проверяющим.
    /// </summary>
    public string? ExaminerId { get; init; }
}