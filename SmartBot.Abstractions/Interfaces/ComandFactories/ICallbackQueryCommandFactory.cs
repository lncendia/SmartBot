using MediatR;
using Telegram.Bot.Types;
using User = SmartBot.Abstractions.Models.Users.User;

namespace SmartBot.Abstractions.Interfaces.ComandFactories;

/// <summary>
/// Интерфейс для создания команд на основе callback-запросов
/// </summary>
public interface ICallbackQueryCommandFactory
{
    /// <summary>
    /// Получает команду на основе пользователя и callback-запроса
    /// </summary>
    /// <param name="user">Пользователь</param>
    /// <param name="query">Callback-запрос</param>
    /// <returns>Команда или null, если команда не найдена</returns>
    IRequest? GetCommand(User user, CallbackQuery query);
}