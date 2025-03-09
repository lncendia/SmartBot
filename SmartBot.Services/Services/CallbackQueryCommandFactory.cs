using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Services.Keyboards.ExaminerKeyboard;
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
        if (user.State == State.Idle && query.Data.StartsWith(ExamKeyboard.ExamReportCallbackData))
        {
            // Извлекаем ID отчёта из данных callback-запроса
            var range = new Range(
                new Index(ExamKeyboard.ExamReportCallbackData.Length), 
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
                    ReportId = reportId
                };
            }

            // Если преобразование не удалось, возвращаем null
            return null;
        }

        // Если пользователь находится в состоянии AwaitingCommentInput и данные соответствуют GoBackCallbackData
        if (user.State is State.AwaitingCommentInput or State.AwaitingExaminerIdForAdding or State.AwaitingExaminerIdForRemoval or State.AwaitingUserIdForRemoval && query.Data == ExamKeyboard.GoBackCallbackData)
        {
            // Возвращаем команду для возврата в состояние Idle
            return new GoBackCommand
            {
                ChatId = query.From.Id,
                MessageId = query.Message!.Id,
                TelegramUserId = query.From.Id,
                User = user,
            };
        }

        // Если ни одно из условий не выполнено, возвращаем null
        return null;
    }
}