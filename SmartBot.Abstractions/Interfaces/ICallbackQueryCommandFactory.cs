using SmartBot.Abstractions.Commands;
using Telegram.Bot.Types;
using User = SmartBot.Abstractions.Models.User;

namespace SmartBot.Abstractions.Interfaces;

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
    TelegramCommand? GetCommand(User user, CallbackQuery query);
}