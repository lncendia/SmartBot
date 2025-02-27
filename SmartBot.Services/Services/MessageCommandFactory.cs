using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using Telegram.Bot.Types;
using User = SmartBot.Abstractions.Models.User;

namespace SmartBot.Services.Services;

/// <summary>
/// Фабрика команд для обработки текстовых сообщений.
/// </summary>
public class MessageCommandFactory : IMessageCommandFactory
{
    /// <summary>
    /// Получает команду для обработки текстового сообщения.
    /// </summary>
    /// <param name="user">Пользователь.</param>
    /// <param name="message">Сообщение.</param>
    /// <returns>Команда для обработки текстового сообщения.</returns>
    public TelegramCommand? GetCommand(User? user, Message message)
    {
        // Если пользователь не найден, возвращаем команду для начала работы
        if (user == null)
            return new StartCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id
            };

        // Если пользователь находится в состоянии ожидания ввода ФИО, возвращаем команду для установки ФИО
        if (user.State == State.AwaitingFullNameInput)
            return new SetFullNameCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                FullName = message.Text
            };

        // Если пользователь находится в состоянии ожидания ввода должности, возвращаем команду для установки должности
        if (user.State == State.AwaitingPositionInput)
            return new SetPositionCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                Position = message.Text
            };

        // Если пользователь находится в состоянии ожидания ввода отчёта, возвращаем команду для анализа отчёта
        if (user.State == State.AwaitingReportInput)
            return new AnalyzeReportCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                Report = message.Text,
            };
        
        // Если пользователь находится в другом состоянии, возвращаем null
        return null;
    }
}