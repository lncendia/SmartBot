using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Services.Keyboards;
using Telegram.Bot.Types;
using IRequest = MediatR.IRequest;
using User = SmartBot.Abstractions.Models.User;

namespace SmartBot.Services.Services;

/// <summary>
/// Фабрика команд для обработки callback-запросов.
/// </summary>
public class CallbackQueryCommandFactory : ICallbackQueryCommandFactory
{
    /// <summary>
    /// Получает команду для обработки callback-запроса на основе данных запроса и состояния пользователя.
    /// </summary>
    /// <param name="user">Пользователь, отправивший callback-запрос.</param>
    /// <param name="query">Callback-запрос.</param>
    /// <returns>Команда для обработки callback-запроса или null, если команда не найдена.</returns>
    public IRequest? GetCommand(User user, CallbackQuery query)
    {
        // Проверяем, есть ли данные в callback-запросе
        if (query.Data == null)
        {
            // Если данных нет, возвращаем null
            return null;
        }

        // Если пользователь находится в состоянии Idle и данные начинаются с префикса ExamReportCallbackData
        if (user.State == State.Idle && query.Data.StartsWith(AdminKeyboard.ExamReportCallbackData))
        {
            // Извлекаем ID отчёта из данных callback-запроса
            var range = new Range(
                new Index(AdminKeyboard.ExamReportCallbackData.Length),
                new Index(query.Data.Length)
            );

            // Пытаемся преобразовать извлечённую строку в Guid
            if (Guid.TryParse(query.Data[range], out var reportId))
            {
                // Если данные соответствуют ExamReportCallbackData, возвращаем команду для начала ввода комментария
                return new StartCommentCommand
                {
                    ChatId = query.From.Id,
                    MessageId = query.Message!.Id,
                    TelegramUserId = query.From.Id,
                    User = user,
                    ReportId = reportId,
                    CallbackQueryId = query.Id
                };
            }

            // Если преобразование не удалось, возвращаем null
            return null;
        }

        // Если пользователь находится в состоянии Idle и данные начинаются с префикса DeleteChatCallbackData
        if (user.State == State.Idle && query.Data.StartsWith(AdminKeyboard.DeleteChatCallbackData))
        {
            // Извлекаем ID отчёта из данных callback-запроса
            var range = new Range(
                new Index(AdminKeyboard.DeleteChatCallbackData.Length),
                new Index(query.Data.Length)
            );

            // Пытаемся преобразовать извлечённую строку в long
            if (long.TryParse(query.Data[range], out var chatId))
            {
                // Если данные соответствуют DeleteChatCallbackData, возвращаем команду для удаления рабочего чата
                return new RemoveWorkingChatCommand
                {
                    ChatId = query.From.Id,
                    TelegramUserId = query.From.Id,
                    User = user,
                    WorkingChatId = chatId,
                    CallbackQueryId = query.Id,
                    MessageId = query.Message!.Id
                };
            }

            // Если преобразование не удалось, возвращаем null
            return null;
        }

        // Если пользователь находится в состоянии Idle и данные начинаются с префикса SelectChatCallbackData
        if (user.State == State.Idle && query.Data.StartsWith(AdminKeyboard.SelectChatCallbackData))
        {
            // Получаем данные из команды
            var data = query.Data.Split('_');

            // Если число данных удовлетворяет параметрам команды StartSelectUserForWorkingChatCommand
            if (data.Length == 2)
            {
                // Пытаемся преобразовать извлечённую строку в long
                if (!long.TryParse(data[1], out var chatId)) return null;

                // Если данные соответствуют SelectChatCallbackData, возвращаем команду начала выбора пользователя для установки рабочего чата
                return new StartSelectUserForWorkingChatCommand
                {
                    ChatId = query.From.Id,
                    TelegramUserId = query.From.Id,
                    User = user,
                    WorkingChatId = chatId,
                    CallbackQueryId = query.Id,
                    MessageId = query.Message!.Id
                };
            }

            // Если число данных удовлетворяет параметрам команды SetWorkingChatCommand
            if (data.Length == 3)
            {
                // Пытаемся преобразовать извлечённую строку в long
                if (!long.TryParse(data[1], out var userId)) return null;

                // Пытаемся преобразовать извлечённую строку в long
                if (!long.TryParse(data[2], out var chatId)) return null;

                // Если данные соответствуют SelectChatCallbackData, возвращаем команду для удаления рабочего чата
                return new SetWorkingChatCommand
                {
                    ChatId = query.From.Id,
                    TelegramUserId = query.From.Id,
                    User = user,
                    WorkingChatId = chatId,
                    UserId = userId,
                    CallbackQueryId = query.Id,
                    MessageId = query.Message!.Id
                };
            }

            // Если число данных не удовлетворяет параметрам команд, возвращаем null
            return null;
        }
        
        // Если пользователь находится в состоянии Idle и данные начинаются с префикса SelectRegularAdminCallbackData
        if (user.State == State.Idle && query.Data.StartsWith(AdminKeyboard.SelectAdminCallbackData))
        {
            // Получаем флаг необходимости добавления администратора как теле-администратора
            var teleAdmin = query.Data == AdminKeyboard.SelectTeleAdminCallbackData;
            
            // Если данные соответствуют SelectAdminCallbackData, возвращаем команду для добавления администратора
            return new StartSelectUserForAssignAdminCommand
            {
                ChatId = query.From.Id,
                TelegramUserId = query.From.Id,
                User = user,
                CallbackQueryId = query.Id,
                MessageId = query.Message!.Id,
                TeleAdmin = teleAdmin
            };
        }

        // Если пользователь находится в состоянии Idle и запрошена команда AddWorkingChatCallbackData
        if (user.State == State.Idle && query.Data == AdminKeyboard.AddWorkingChatCallbackData)
        {
            // Если данные соответствуют AddWorkingChatCallbackData, возвращаем команду для добавления рабочего чата
            return new StartAddWorkingChatCommand
            {
                ChatId = query.From.Id,
                TelegramUserId = query.From.Id,
                User = user,
                CallbackQueryId = query.Id,
                MessageId = query.Message!.Id
            };
        }

        // Если пользователь находится в состоянии Idle и запрошена команда RemoveWorkingChatCallbackData
        if (user.State == State.Idle && query.Data == AdminKeyboard.RemoveWorkingChatCallbackData)
        {
            // Если данные соответствуют RemoveWorkingChatCallbackData, возвращаем команду для добавления рабочего чата
            return new StartRemoveWorkingChatCommand
            {
                ChatId = query.From.Id,
                TelegramUserId = query.From.Id,
                User = user,
                CallbackQueryId = query.Id,
                MessageId = query.Message!.Id
            };
        }

        // Если пользователь находится в состоянии Idle и запрошена команда AssignAdminCallbackData
        if (user.State == State.Idle && query.Data == AdminKeyboard.AssignAdminCallbackData)
        {
            // Если данные соответствуют AssignAdminCallbackData, возвращаем команду для добавления рабочего чата
            return new StartAssignAdminCommand
            {
                ChatId = query.From.Id,
                TelegramUserId = query.From.Id,
                User = user,
                CallbackQueryId = query.Id,
                MessageId = query.Message!.Id
            };
        }

        // Если пользователь находится в состоянии Idle и запрошена команда DemoteAdminCallbackData
        if (user.State == State.Idle && query.Data == AdminKeyboard.DemoteAdminCallbackData)
        {
            // Если данные соответствуют DemoteAdminCallbackData, возвращаем команду для добавления рабочего чата
            return new StartDemoteAdminCommand
            {
                ChatId = query.From.Id,
                TelegramUserId = query.From.Id,
                User = user,
                CallbackQueryId = query.Id,
                MessageId = query.Message!.Id
            };
        }

        // Если пользователь находится в состоянии Idle и запрошена команда BlockUserCallbackData
        if (user.State == State.Idle && query.Data == AdminKeyboard.BlockUserCallbackData)
        {
            // Если данные соответствуют BlockUserCallbackData, возвращаем команду для добавления рабочего чата
            return new StartBlockUserCommand
            {
                ChatId = query.From.Id,
                TelegramUserId = query.From.Id,
                User = user,
                CallbackQueryId = query.Id,
                MessageId = query.Message!.Id
            };
        }

        // Если пользователь находится в состоянии Idle и запрошена команда SetWorkingChatCallbackData
        if (user.State == State.Idle && query.Data == AdminKeyboard.SetWorkingChatCallbackData)
        {
            // Если данные соответствуют AddWorkingChatCallbackData, возвращаем команду для добавления рабочего чата
            return new StartSetWorkingChatCommand
            {
                ChatId = query.From.Id,
                TelegramUserId = query.From.Id,
                User = user,
                CallbackQueryId = query.Id,
                MessageId = query.Message!.Id
            };
        }

        // Если пользователь находится в состоянии, позволяющем вернуться назад и данные соответствуют GoBackCallbackData
        if (user.State is State.AwaitingCommentInput or State.AwaitingAdminIdForAdding
                or State.AwaitingTeleAdminIdForAdding or State.AwaitingAdminIdForRemoval
                or State.AwaitingUserIdForBlock or State.AwaitingWorkingChatForAdding
                or State.Idle or State.AwaitingUserIdForSetWorkingChat
            && query.Data == AdminKeyboard.AdminGoBackCallbackData)
        {
            // Возвращаем команду для возврата в состояние Idle
            return new GoBackCommand
            {
                ChatId = query.From.Id,
                MessageId = query.Message!.Id,
                TelegramUserId = query.From.Id,
                User = user,
                CallbackQueryId = query.Id
            };
        }

        // Если ни одно из условий не выполнено, возвращаем null
        return null;
    }
}