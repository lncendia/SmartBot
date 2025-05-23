using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces.ComandFactories;
using SmartBot.Services.Keyboards;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using IRequest = MediatR.IRequest;
using User = SmartBot.Abstractions.Models.Users.User;

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

        // Проверяем, находится ли пользователь в состоянии Idle или AwaitingReportInput
        if (user.State is State.Idle or State.AwaitingReportInput)
        {
            // Проверяем, является ли callback-данные командой ответа на комментарий
            if (query.Data.StartsWith(DefaultKeyboard.AnswerCallbackData))
            {
                // Разбиваем callback-данные на составные части по разделителю '_'
                var data = query.Data.Split('_');

                // Валидация: проверяем, что данные содержат 4 элемента (префикс, reportId, userId, eveningReport)
                if (data.Length != 4) return null;

                // Парсим второй элемент данных как Guid (идентификатор отчета)
                if (!Guid.TryParse(data[1], out var reportId)) return null;

                // Парсим второй элемент данных как long (идентификатор пользователя)
                if (!long.TryParse(data[2], out var userId)) return null;

                // Парсим третий элемент данных как boolean (флаг вечернего отчета)
                if (!bool.TryParse(data[3], out var eveningReport)) return null;

                // Создаем и возвращаем команду для начала ввода комментария к отчету
                return new StartAnswerCommand
                {
                    ChatId = query.From.Id,
                    MessageId = query.Message!.Id,
                    TelegramUserId = query.From.Id,
                    User = user,
                    CallbackQueryId = query.Id,
                    UserId = userId,
                    ReportId = reportId,
                    EveningReport = eveningReport,
                    Message = query.Message.ToHtml()
                };
            }
        }

        // Проверяем, находится ли пользователь в состоянии AwaitingReportInput
        if (user.State is State.AwaitingReportInput && query.Message?.ReplyToMessage?.MessageId != null)
        {
            // Проверяем, является ли callback-данные командой для отправки отчёта без проверки анализатором
            if (query.Data == DefaultKeyboard.SendForManualAnalysisCallbackData)
            {
                // Создаем и возвращаем команду для отправки отчёта без проверки анализатором
                return new ManualReportAnalysisCommand
                {
                    ChatId = query.From.Id,
                    MessageId = query.Message!.Id,
                    TelegramUserId = query.From.Id,
                    User = user,
                    CallbackQueryId = query.Id,
                    ReportMessageId = query.Message.ReplyToMessage.MessageId
                };
            }

            // Проверяем, является ли callback-данные командой для отправки отчёта на повторный анализ
            if (query.Data == DefaultKeyboard.RepeatAnalysisCallbackData)
            {
                // Создаем и возвращаем команду для отправки отчёта на повторный анализ
                return new RepeatReportAnalysisCommand
                {
                    ChatId = query.From.Id,
                    MessageId = query.Message!.Id,
                    TelegramUserId = query.From.Id,
                    User = user,
                    CallbackQueryId = query.Id,
                    ReportMessageId = query.Message.ReplyToMessage.MessageId
                };
            }
        }

        // Обработка команды отмены при вводе комментария или ответа
        if (user.State is State.AwaitingCommentInput or State.AwaitingAnswerInput or State.AwaitingRejectCommentInput
            && query.Data == DefaultKeyboard.CancelCallbackData)
        {
            // Создаем команду отмены текущего действия
            return new CancelCommand
            {
                ChatId = query.From.Id,
                MessageId = query.Message!.Id,
                TelegramUserId = query.From.Id,
                User = user,
                CallbackQueryId = query.Id
            };
        }

        if (user.State is State.Idle or State.AwaitingReportInput && user.IsAdmin)
        {
            // Проверяем, является ли callback-данные командой для комментирования отчета
            if (query.Data.StartsWith(AdminKeyboard.CommentReportCallbackData))
            {
                // Разбиваем callback-данные на составные части по разделителю '_'
                var data = query.Data.Split('_');

                // Валидация: проверяем, что данные содержат 3 элемента (префикс, reportId, eveningReport)
                if (data.Length != 3) return null;

                // Парсим второй элемент данных как Guid (идентификатор отчета)
                if (!Guid.TryParse(data[1], out var reportId)) return null;

                // Парсим третий элемент данных как boolean (флаг вечернего отчета)
                if (!bool.TryParse(data[2], out var eveningReport)) return null;

                // Создаем и возвращаем команду для начала ввода комментария к отчету
                return new StartCommentCommand
                {
                    ChatId = query.From.Id,
                    MessageId = query.Message!.Id,
                    TelegramUserId = query.From.Id,
                    User = user,
                    ReportId = reportId,
                    CallbackQueryId = query.Id,
                    EveningReport = eveningReport
                };
            }

            // Проверяем, является ли callback-данные командой для подтверждения отчёта
            if (query.Data.StartsWith(AdminKeyboard.ApproveReportCallbackData))
            {
                // Разбиваем callback-данные на составные части по разделителю '_'
                var data = query.Data.Split('_');

                // Валидация: проверяем, что данные содержат 3 элемента (префикс, reportId, eveningReport)
                if (data.Length != 3) return null;

                // Парсим второй элемент данных как Guid (идентификатор отчета)
                if (!Guid.TryParse(data[1], out var reportId)) return null;

                // Парсим третий элемент данных как boolean (флаг вечернего отчета)
                if (!bool.TryParse(data[2], out var eveningReport)) return null;

                // Создаем и возвращаем команду для начала ввода комментария к отчету
                return new ApproveReportCommand
                {
                    ChatId = query.From.Id,
                    MessageId = query.Message!.Id,
                    TelegramUserId = query.From.Id,
                    User = user,
                    ReportId = reportId,
                    CallbackQueryId = query.Id,
                    EveningReport = eveningReport
                };
            }

            // Проверяем, является ли callback-данные командой для отклонения отчёта
            if (query.Data.StartsWith(AdminKeyboard.RejectReportCallbackData))
            {
                // Разбиваем callback-данные на составные части по разделителю '_'
                var data = query.Data.Split('_');

                // Валидация: проверяем, что данные содержат 3 элемента (префикс, reportId, eveningReport)
                if (data.Length != 3) return null;

                // Парсим второй элемент данных как Guid (идентификатор отчета)
                if (!Guid.TryParse(data[1], out var reportId)) return null;

                // Парсим третий элемент данных как boolean (флаг вечернего отчета)
                if (!bool.TryParse(data[2], out var eveningReport)) return null;

                // Создаем и возвращаем команду для начала ввода комментария к отчету
                return new StartRejectReportCommand
                {
                    ChatId = query.From.Id,
                    MessageId = query.Message!.Id,
                    TelegramUserId = query.From.Id,
                    User = user,
                    ReportId = reportId,
                    CallbackQueryId = query.Id,
                    EveningReport = eveningReport
                };
            }

            // Если данные начинаются с префикса DeleteChatCallbackData
            if (query.Data.StartsWith(AdminKeyboard.DeleteChatCallbackData))
            {
                // Извлекаем ID отчёта из данных callback-запроса
                var range = new Range(
                    new Index(AdminKeyboard.DeleteChatCallbackData.Length),
                    new Index(query.Data.Length)
                );

                // Пытаемся преобразовать извлечённую строку в long
                if (!long.TryParse(query.Data[range], out var chatId)) return null;

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

            // Если данные начинаются с префикса SelectChatCallbackData
            if (query.Data.StartsWith(AdminKeyboard.SelectChatCallbackData))
            {
                // Получаем данные из команды
                var data = query.Data.Split('_');

                // Если число данных удовлетворяет параметрам команды StartSelectUserForWorkingChatCommand
                if (data.Length == 3)
                {
                    // Пытаемся преобразовать извлечённую строку в long
                    if (!long.TryParse(data[2], out var chatId)) return null;

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
                if (data.Length == 4)
                {
                    // Пытаемся преобразовать извлечённую строку в long
                    if (!long.TryParse(data[2], out var userId)) return null;

                    // Пытаемся преобразовать извлечённую строку в long
                    if (!long.TryParse(data[3], out var chatId)) return null;

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

            // Если данные начинаются с префикса SelectAdminCallbackData
            if (query.Data.StartsWith(AdminKeyboard.SelectAdminCallbackData))
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

            // Если запрошена команда AddWorkingChatCallbackData
            if (query.Data == AdminKeyboard.AddWorkingChatCallbackData)
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

            // Если запрошена команда RemoveWorkingChatCallbackData
            if (query.Data == AdminKeyboard.RemoveWorkingChatCallbackData)
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

            // Если запрошена команда AssignAdminCallbackData
            if (query.Data == AdminKeyboard.AssignAdminCallbackData)
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

            // Если запрошена команда DemoteAdminCallbackData
            if (query.Data == AdminKeyboard.DemoteAdminCallbackData)
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

            // Если запрошена команда BlockUserCallbackData
            if (query.Data == AdminKeyboard.BlockUserCallbackData)
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

            // Если запрошена команда SetWorkingChatCallbackData
            if (query.Data == AdminKeyboard.SetWorkingChatCallbackData)
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
        }

        // Обработка команды "Назад" для административных состояний
        if (user.State is State.AwaitingAdminIdForAdding or State.AwaitingTeleAdminIdForAdding
                or State.AwaitingAdminIdForRemoval or State.AwaitingUserIdForBlock
                or State.AwaitingWorkingChatIdForAdding or State.Idle
                or State.AwaitingUserIdForSetWorkingChat
            && query.Data == AdminKeyboard.GoBackCallbackData)
        {
            // Создаем команду возврата в предыдущее меню
            return new AdminGoBackCommand
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