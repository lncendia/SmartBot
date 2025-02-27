using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using Telegram.Bot.Types;
using User = SmartBot.Abstractions.Models.User;

namespace SmartBot.Services.Services;

/// <summary>
/// Фабрика команд для обработки callback-запросов.
/// </summary>
public class CallbackQueryCommandFactory : ICallbackQueryCommandFactory
{
    /// <summary>
    /// Получает команду для обработки callback-запроса.
    /// </summary>
    /// <param name="user">Пользователь.</param>
    /// <param name="query">Callback-запрос.</param>
    /// <returns>Команда для обработки callback-запроса.</returns>
    public TelegramCommand? GetCommand(User user, CallbackQuery query)
    {
        return null;
    }
}