using SmartBot.Abstractions.Commands;
using Telegram.Bot.Types;
using User = SmartBot.Abstractions.Models.User;

namespace SmartBot.Abstractions.Interfaces;

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
    TelegramCommand? GetCommand(User? user, Message message);
}