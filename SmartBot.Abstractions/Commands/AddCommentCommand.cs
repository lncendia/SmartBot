using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;
    
/// <summary>
/// Команда для добавления комментария к отчёту.
/// </summary>
public class AddCommentCommand : TelegramCommand
{
    /// <summary>
    /// Текст комментария.
    /// </summary>
    public string? Comment { get; init; }
}