using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для начала процесса выбора пользователя для установки рабочего чата пользователю.
/// </summary>
public class StartSelectUserForWorkingChatCommand : AdminCallbackQuery
{
    /// <summary>
    /// Идентификатор рабочего чата, который необходимо установить пользователю.
    /// </summary>
    public required long WorkingChatId { get; init; }
}