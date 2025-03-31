namespace SmartBot.Abstractions.Models.WorkingChats;

/// <summary>
/// Класс, представляющий рабочий чат для отправки отчетов
/// </summary>
/// <remarks>
/// Содержит информацию о чатах, в которых сотрудники отправляют ежедневные отчеты.
/// Используется для организации рабочих пространств и разграничения отчетности.
/// </remarks>
public class WorkingChat
{
    /// <summary>
    /// Уникальный идентификатор чата в Telegram
    /// </summary>
    /// <value>
    /// Положительное число, соответствующее chat_id в Telegram API.
    /// Для групповых чатов - отрицательное число.
    /// </value>
    public required long Id { get; init; }
    
    /// <summary>
    /// Название рабочего чата
    /// </summary>
    /// <value>
    /// Человекочитаемое название чата (например: "Отдел разработки - Утренние отчеты").
    /// Максимальная длина - 150 символов.
    /// </value>
    public required string Name { get; set; }
    
    /// <summary>
    /// Уникальный идентификатор для целевой ветки сообщений (топика) форума.
    /// </summary>
    public int? MessageThreadId { get; set; }
}