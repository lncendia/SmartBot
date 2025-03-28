using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для добавления нового рабочего чата в систему
/// </summary>
/// <remarks>
/// Используется администраторами для регистрации новых чатов,
/// в которых будут публиковаться отчеты сотрудников
/// </remarks>
public class AddWorkingChatCommand : TelegramCommand
{
    /// <summary>
    /// Идентификатор чата в Telegram
    /// </summary>
    /// <value>
    /// Положительное число - ID чата в Telegram API
    /// </value>
    public required long WorkingChatId { get; init; }

    /// <summary>
    /// Название рабочего чата
    /// </summary>
    /// <value>
    /// Человекочитаемое название чата (например: "Основной рабочий чат отдела продаж")
    /// </value>
    public required string WorkingChatName { get; init; }
}