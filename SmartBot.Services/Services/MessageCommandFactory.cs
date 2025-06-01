using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces.ComandFactories;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using IRequest = MediatR.IRequest;
using User = SmartBot.Abstractions.Models.Users.User;

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
            // Если это не приватный чат - возвращаем null.
            if (message.Chat.Type != ChatType.Private) return null;

            // Если пользователь не найден, возвращаем команду для начала работы
            return new StartCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id
            };
        }

        // Если пользователь запрашивает добавление текущего чата как рабочего
        if (user.State is State.Idle or State.AwaitingReportInput && message.Text == "/addworkingchat")
        {
            // Если это не группа - возвращаем null.
            if (message.Chat.Type is not ChatType.Group and not ChatType.Supergroup) return null;
            
            // Возвращаем команду добавления текущего чата как рабочего
            return new AddWorkingChatFromMessageCommand
            {
                ChatId = message.Chat,
                User = user,
                TelegramUserId = message.From!.Id,
                WorkingChatId = message.Chat.Id,
                WorkingChatName = message.Chat.Title!,
                MessageId = message.MessageId,
                MessageThreadId = message.MessageThreadId
            };
        }

        // Дальнейшие команды только для приватных чатов, если чат не приватный - возвращаем null.
        if (message.Chat.Type != ChatType.Private) return null;

        // Если пользователь запрашивает вход в панель администратора
        if (user.State is State.Idle or State.AwaitingReportInput && message.Text == "/admin")
        {
            // Возвращаем команду входа в панель администратора
            return new AdminCommand
            {
                ChatId = message.Chat,
                User = user,
                TelegramUserId = message.From!.Id
            };
        }

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
                Username = message.From.Username,
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
                Username = message.From.Username,
                User = user,
                MessageId = message.Id,
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

        // Если бот ожидает ввода ответа на комментарий
        if (user.State == State.AwaitingAnswerInput)
        {
            // Возвращаем команду для ответа на комментарий
            return new AnswerCommand
            {
                ChatId = message.Chat,
                TelegramUserId = message.From!.Id,
                User = user,
                Message = message.Text
            };
        }

        // Если бот ожидает ввода причины отклонения отчёта
        if (user.State == State.AwaitingRejectCommentInput)
        {
            // Возвращаем команду для добавления комментария
            return new RejectReportCommand
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
        if (user.State == State.AwaitingWorkingChatIdForAdding && message.Type == MessageType.ChatShared)
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

        // Если состояние пользователя не соответствует ни одному из вышеперечисленных, возвращаем null
        return null;
    }
}