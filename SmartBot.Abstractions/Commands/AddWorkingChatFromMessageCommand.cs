using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для добавления нового рабочего чата в систему через команду в этом чате.
/// </summary>
/// <remarks>
/// Используется администраторами для регистрации новых чатов,
/// в которых будут публиковаться отчеты сотрудников
/// </remarks>
public class AddWorkingChatFromMessageCommand : TelegramCommand
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
    
    /// <summary>
    /// Уникальный идентификатор для целевой ветки сообщений (топика) форума.
    /// </summary>
    public int? MessageThreadId { get; init; }
    
    /// <summary>
    /// Идентификатор сообщения
    /// </summary>
    public required int MessageId { get; init; }
}