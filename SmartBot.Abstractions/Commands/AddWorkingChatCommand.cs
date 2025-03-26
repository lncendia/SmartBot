using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// 
/// </summary>
public class AddWorkingChatCommand : TelegramCommand
{
    /// <summary>
    /// 
    /// </summary>
    public required long WorkingChatId { get; init; }

    /// <summary>
    /// 
    /// </summary>
    public required string WorkingChatName { get; init; }
}