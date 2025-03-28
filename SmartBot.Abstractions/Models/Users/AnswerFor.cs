namespace SmartBot.Abstractions.Models.Users;

/// <summary>
/// Класс, содержащий информацию о сообщении, на которое дается ответ
/// </summary>
/// <remarks>
/// Используется для хранения контекста ответа пользователя на предыдущее сообщение/отчет
/// </remarks>
public class AnswerFor
{
    /// <summary>
    /// Идентификатор пользователя, которому адресован ответ
    /// </summary>
    public required long ToUserId { get; init; }

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
    /// Текст оригинального сообщения, на которое дается ответ
    /// </summary>
    public required string Message { get; init; }
}