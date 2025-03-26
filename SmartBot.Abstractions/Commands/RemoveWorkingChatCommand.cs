using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для удаления рабочего чата.
/// </summary>
public class RemoveWorkingChatCommand : AdminCallbackQuery
{
    /// <summary>
    /// Идентификатор рабочего чата, который нужно удалить.
    /// </summary>
    public required long WorkingChatId { get; init; }
}