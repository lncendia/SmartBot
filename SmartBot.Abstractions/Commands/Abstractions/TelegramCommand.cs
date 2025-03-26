using MediatR;
using Telegram.Bot.Types;
using User = SmartBot.Abstractions.Models.User;

namespace SmartBot.Abstractions.Commands.Abstractions;

/// <summary>
/// Абстрактная команда для Telegram
/// </summary>
public abstract class TelegramCommand : IRequest
{
    /// <summary>
    /// Идентификатор чата
    /// </summary>
    public required ChatId ChatId { get; init; }

    /// <summary>
    /// Идентификатор пользователя в Telegram
    /// </summary>
    public required long TelegramUserId { get; init; }

    /// <summary>
    /// Пользователь
    /// </summary>
    public User? User { get; init; }
}