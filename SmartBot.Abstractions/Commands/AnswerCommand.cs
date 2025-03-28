using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;
    
/// <summary>
/// Команда для отправки ответа на сообщение пользователя
/// </summary>
public class AnswerCommand : TelegramCommand
{
    /// <summary>
    /// Текст ответного сообщения
    /// </summary>
    /// <value>
    /// Максимальная длина - 2000 символов.
    /// </value>
    public string? Message { get; init; }
}