﻿using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using IRequest = MediatR.IRequest;
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
    public IRequest? GetCommand(User? user, Message message)
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

        // Если бот ожидает ввода ФИО
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

        // Если бот ожидает ввода должности
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

        // Если бот ожидает ввода отчёта
        if (user.State == State.AwaitingReportInput)
        {
            // Возвращаем команду для анализа отчёта
            return new AnalyzeReportCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                Report = message.Text
            };
        }

        // Если бот ожидает ввода комментария
        if (user.State == State.AwaitingCommentInput)
        {
            // Возвращаем команду для добавления комментария
            return new AddCommentCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                Comment = message.Text
            };
        }

        // Если бот ожидает ввода ID нового администратора
        if (user.State is State.AwaitingAdminIdForAdding or State.AwaitingTeleAdminIdForAdding &&
            message.Type == MessageType.UsersShared)
        {
            // Получаем идентификатор пользователя
            var userId = message.UsersShared?.Users.FirstOrDefault()?.UserId;

            // Если идентификатор не удалось получить - не выполняем команду
            if (userId == null) return null;

            // Получаем флаг необходимости добавления администратора как теле-администратора
            var teleAdmin = user.State == State.AwaitingTeleAdminIdForAdding;
            
            // Возвращаем команду для добавления администратора
            return new AssignAdminCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                AdminId = userId.Value,
                TeleAdmin = teleAdmin
            };
        }

        // Если бот ожидает ввода ID администратора для удаления
        if (user.State == State.AwaitingAdminIdForRemoval && message.Type == MessageType.UsersShared)
        {
            // Получаем идентификатор пользователя
            var userId = message.UsersShared?.Users.FirstOrDefault()?.UserId;

            // Если идентификатор не удалось получить - не выполняем команду
            if (userId == null) return null;

            // Возвращаем команду для удаления администратора
            return new DemoteAdminCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                AdminId = userId.Value
            };
        }

        // Если бот ожидает ввода ID пользователя для установки рабочего чата
        if (user.State == State.AwaitingUserIdForSetWorkingChat && message.Type == MessageType.UsersShared)
        {
            // Получаем идентификатор пользователя
            var userId = message.UsersShared?.Users.FirstOrDefault()?.UserId;

            // Если идентификатор не удалось получить - не выполняем команду
            if (userId == null) return null;

            // Возвращаем команду для установки рабочего чата
            return new SetWorkingChatFromSelectedCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                UserId = userId.Value
            };
        }

        // Если бот ожидает ввода ID пользователя для удаления
        if (user.State == State.AwaitingUserIdForBlock && message.Type == MessageType.UsersShared)
        {
            // Получаем идентификатор пользователя
            var userId = message.UsersShared?.Users.FirstOrDefault()?.UserId;

            // Если идентификатор не удалось получить - не выполняем команду
            if (userId == null) return null;

            // Возвращаем команду для удаления пользователя
            return new BlockUserCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                UserId = userId.Value
            };
        }

        // Если бот ожидает ввода ID пользователя для удаления
        if (user.State == State.AwaitingWorkingChatForAdding && message.Type == MessageType.ChatShared)
        {
            // Получаем идентификатор чата
            var chatId = message.ChatShared?.ChatId;

            // Получаем имя чата
            var title = message.ChatShared?.Title;

            // Если идентификатор или имя не удалось получить - не выполняем команду
            if (chatId == null || title == null) return null;

            // Возвращаем команду для удаления пользователя
            return new AddWorkingChatCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                WorkingChatId = chatId.Value,
                WorkingChatName = title
            };
        }


        // Если пользователь находится в состоянии Idle (ожидания команд)
        if (user.State == State.Idle)
        {
            // Возвращаем команду в зависимости от текста сообщения
            return message.Text switch
            {
                // Если текст сообщения "/admin", возвращаем команду для входа в панель администратора
                "/admin" => new AdminCommand
                {
                    ChatId = message.Chat,
                    User = user,
                    TelegramUserId = message.From!.Id
                },

                // Если текст сообщения не соответствует ни одной команде, возвращаем null
                _ => null
            };
        }

        // Если состояние пользователя не соответствует ни одному из вышеперечисленных, возвращаем null
        return null;
    }
}