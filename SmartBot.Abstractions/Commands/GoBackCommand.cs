using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для возврата в состояние Idle (отмена текущего действия).
/// </summary>
public class GoBackCommand : AdminCallbackQuery;