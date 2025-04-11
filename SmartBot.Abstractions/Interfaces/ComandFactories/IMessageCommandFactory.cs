using Telegram.Bot.Types;
using IRequest = MediatR.IRequest;
using User = SmartBot.Abstractions.Models.Users.User;

namespace SmartBot.Abstractions.Interfaces.ComandFactories;

/// <summary>
/// Интерфейс для создания команд на основе сообщений
/// </summary> 
public interface IMessageCommandFactory
{
    /// <summary>
    /// Получает команду на основе пользователя и сообщения
    /// </summary>
    /// <param name="user">Пользователь</param>
    /// <param name="message">Сообщение</param>
    /// <returns>Команда или null, если команда не найдена</returns>
    IRequest? GetCommand(User? user, Message message);
}