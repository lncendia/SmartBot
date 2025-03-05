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
    /// Получает команду для обработки текстового сообщения на основе состояния пользователя.
    /// </summary>
    /// <param name="user">Пользователь, отправивший сообщение.</param>
    /// <param name="message">Текстовое сообщение.</param>
    /// <returns>Команда для обработки сообщения или null, если команда не найдена.</returns>
    public TelegramCommand? GetCommand(User? user, Message message)
    {
        // Проверяем, существует ли пользователь
        if (user == null)
        {
            // Если пользователь не найден, возвращаем команду для начала работы
            return new StartCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id
            };
        }

        // Проверяем состояние пользователя и возвращаем соответствующую команду

        // Если пользователь ожидает ввода ФИО
        if (user.State == State.AwaitingFullNameInput)
        {
            // Возвращаем команду для установки ФИО
            return new SetFullNameCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                FullName = message.Text
            };
        }

        // Если пользователь ожидает ввода должности
        if (user.State == State.AwaitingPositionInput)
        {
            // Возвращаем команду для установки должности
            return new SetPositionCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                Position = message.Text
            };
        }

        // Если пользователь ожидает ввода отчёта
        if (user.State == State.AwaitingReportInput)
        {
            // Возвращаем команду для анализа отчёта
            return new AnalyzeReportCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                Report = message.Text,
            };
        }

        // Если пользователь ожидает ввода комментария
        if (user.State == State.AwaitingCommentInput)
        {
            // Возвращаем команду для добавления комментария
            return new AddCommentCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                Comment = message.Text,
            };
        }

        // Если пользователь ожидает ввода ID нового проверяющего
        if (user.State == State.AwaitingExaminerIdForAdding)
        {
            // Возвращаем команду для добавления проверяющего
            return new AddExaminerCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                ExaminerId = message.Text
            };
        }

        // Если пользователь ожидает ввода ID проверяющего для удаления
        if (user.State == State.AwaitingExaminerIdForRemoval)
        {
            // Возвращаем команду для удаления проверяющего
            return new RemoveExaminerCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                ExaminerId = message.Text
            };
        }

        // Если пользователь ожидает ввода ID пользователя для удаления
        if (user.State == State.AwaitingUserIdForRemoval)
        {
            // Возвращаем команду для удаления пользователя
            return new RemoveUserCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                UserId = message.Text
            };
        }

        // Если пользователь находится в состоянии Idle (ожидания команд)
        if (user.State == State.Idle)
        {
            // Возвращаем команду в зависимости от текста сообщения
            return message.Text switch
            {
                // Если текст сообщения "/block", возвращаем команду для начала удаления пользователя
                "/block" => new StartRemoveUserCommand
                {
                    ChatId = message.Chat,
                    TelegramUserId = message.From!.Id,
                    User = user,
                },

                // Если текст сообщения "/examineradd", возвращаем команду для начала добавления проверяющего
                "/examineradd" => new StartAddExaminerCommand
                {
                    ChatId = message.Chat,
                    TelegramUserId = message.From!.Id,
                    User = user,
                },

                // Если текст сообщения "/examinerremove", возвращаем команду для начала удаления проверяющего
                "/examinerremove" => new StartRemoveExaminerCommand
                {
                    ChatId = message.Chat,
                    TelegramUserId = message.From!.Id,
                    User = user,
                },

                // Если текст сообщения не соответствует ни одной команде, возвращаем null
                _ => null
            };
        }

        // Если состояние пользователя не соответствует ни одному из вышеперечисленных, возвращаем null
        return null;
    }
}