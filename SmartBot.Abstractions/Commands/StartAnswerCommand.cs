using SmartBot.Abstractions.Commands.Abstractions;
using SmartBot.Abstractions.Models;
using SmartBot.Abstractions.Models.Users;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для начала процесса ответа на сообщение.
/// </summary>
public class StartAnswerCommand : AdminCallbackQuery
{
    /// <summary>
    /// Идентификатор пользователя, которому адресован ответ
    /// </summary>
    public required long UserId { get; init; }

    /// <summary>
    /// Идентификатор отчета, к которому относится ответ
    /// </summary>
    public required Guid ReportId { get; init; }

    /// <summary>
    /// Флаг, указывающий на тип отчета (утренний/вечерний)
    /// </summary>
    /// <value>
    /// true - вечерний отчет, false - утренний отчет
    /// </value>
    public required bool EveningReport { get; init; }
    
    /// <summary>
    /// Текст сообщения с оригинальным текстом, на который дается ответ, включая форматирование
    /// </summary>
    public string? Message { get; init; }
}