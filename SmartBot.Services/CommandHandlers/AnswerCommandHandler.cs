using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Models.Reports;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = SmartBot.Abstractions.Models.Users.User;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для добавления ответа к отчёту.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="logger">Логгер для записи событий.</param>
public class AnswerCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    ILogger<AnswerCommandHandler> logger)
    : IRequestHandler<AnswerCommand>
{
    /// <summary>
    /// Максимально допустимая длина ответа
    /// </summary>
    private const int MaxAnswerLength = 2000;

    /// <summary>
    /// Сообщение об ошибке: отчёт не найден
    /// </summary>
    private const string ReportNotFoundMessage = "<b>❌ Ошибка:</b> Отчёт не найден. Возможно, он был удалён.";

    /// <summary>
    /// Сообщение об ошибке: пустой ответ
    /// </summary>
    private const string EmptyAnswerMessage =
        "<b>❌ Пустой ответ</b>\n\n" +
        "Ответ не может состоять только из пробелов. Пожалуйста, введите содержательный текст.";

    /// <summary>
    /// Сообщение об ошибке: ответ слишком длинный
    /// </summary>
    private readonly string _answerTooLongMessage =
        $"<b>❌ Слишком длинный ответ</b>\n\n" +
        $"Максимальная длина ответа - {MaxAnswerLength} символов. Сократите текст и попробуйте снова.";

    /// <summary>
    /// Сообщение об успешной отправке ответа
    /// </summary>
    private const string AnswerSentSuccessMessage = "<b>✅ Ответ успешно отправлен.</b>";

    /// <summary>
    /// Сообщение о заблокированном пользователе
    /// </summary>
    private const string UserBlockedMessage = "<b>❌ Ошибка:</b> Пользователь заблокирован.";

    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не найден.
    /// </summary>
    private const string UserNotFoundMessage = "<b>❌ Ошибка:</b> Пользователь с указанным ID не найден.";

    /// <summary>
    /// Формат сообщения с отчётом пользователя
    /// </summary>
    private const string ReportMessageFormat =
        "📝 <b>Отчёт от</b> <i>{0}</i>\n" +
        "👤 <b>Должность:</b> <i>{1}</i>\n\n" +
        "👇 <b>Текст отчёта:</b>\n" +
        "<blockquote>{2}</blockquote>\n\n";
    
    /// <summary>
    /// Формат сообщения с отчётом пользователя
    /// </summary>
    private const string YourReportMessageFormat =
        "👇 <b>Ваш отчёт:</b>\n" +
        "<blockquote>{0}</blockquote>\n\n";

    /// <summary>
    /// Формат сообщения с комментарием к отчёту
    /// </summary>
    private const string AnswerMessageFormat =
        "<b>💬 Вы:</b>\n" +
        "<blockquote>{0}</blockquote>\n\n" +
        "<b>💬 {1}</b> ({2}):\n" +
        "<blockquote>{3}</blockquote>";

    /// <summary>
    /// Обрабатывает команду добавления ответа к отчёту
    /// </summary>
    /// <param name="request">Данные запроса</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task Handle(AnswerCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, что текст ответа не пустой
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            // Отправляем сообщение об ошибке пустого ответа
            await client.SendMessage(
                chatId: request.ChatId,
                text: EmptyAnswerMessage,
                parseMode: ParseMode.Html,
                replyMarkup: DefaultKeyboard.CancelKeyboard,
                cancellationToken: cancellationToken
            );
            
            // Завершаем выполнение метода
            return;
        }

        // Проверяем длину ответа
        if (request.Message.Length > MaxAnswerLength)
        {
            // Отправляем сообщение о превышении длины
            await client.SendMessage(
                chatId: request.ChatId,
                text: _answerTooLongMessage,
                parseMode: ParseMode.Html,
                replyMarkup: DefaultKeyboard.CancelKeyboard,
                cancellationToken: cancellationToken
            );
            
            // Завершаем выполнение метода
            return;
        }

        // Определяем новое состояние пользователя
        var newState = request.User!.IsEmployee
            ? State.AwaitingReportInput 
            : State.Idle;
        
        // Проверяем существование отчёта
        if (request.User.AnswerFor == null)
        {
            // Обновляем состояние пользователя и уведомляем об отсутствии отчёта
            await UpdateStateAndSendMessageAsync(
                request: request,
                newState: newState,
                message: ReportNotFoundMessage,
                cancellationToken: cancellationToken
            );
            
            // Завершаем выполнение метода
            return;
        }

        // Получаем отчёт из базы данных
        var report = await unitOfWork.Query<Report>()
            .FirstOrDefaultAsync(r => r.Id == request.User!.AnswerFor.ReportId, cancellationToken);

        // Проверяем существование отчёта
        if (report == null)
        {
            // Обновляем состояние пользователя и уведомляем об отсутствии отчёта
            await UpdateStateAndSendMessageAsync(
                request: request,
                newState: newState,
                message: ReportNotFoundMessage,
                cancellationToken: cancellationToken
            );
            
            // Завершаем выполнение метода
            return;
        }

        // Получаем пользователя, которому отвечаем
        var answerTo = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(u => u.Id == request.User!.AnswerFor.ToUserId, cancellationToken);

        // Проверяем существование пользователя
        if (answerTo == null)
        {
            // Обновляем состояние пользователя и уведомляем об отсутствии пользователя
            await UpdateStateAndSendMessageAsync(
                request: request,
                newState: newState,
                message: UserNotFoundMessage,
                cancellationToken: cancellationToken
            );
            
            // Завершаем выполнение метода
            return;
        }

        // Проверяем статус пользователя
        if (answerTo.Role == Role.Blocked)
        {
            // Обновляем состояние пользователя и уведомляем о блокировке пользователя
            await UpdateStateAndSendMessageAsync(
                request: request,
                newState: newState,
                message: UserBlockedMessage,
                cancellationToken: cancellationToken
            );
            
            // Завершаем выполнение метода
            return;
        }

        // Запоминаем данные сообщения, на которое пользователь отвечает для дальнейшей отправки ответного сообщения
        var answerFor = request.User!.AnswerFor;
        
        // Обновляем состояние пользователя и уведомляем об успехе
        await UpdateStateAndSendMessageAsync(
            request: request,
            newState: newState,
            message: AnswerSentSuccessMessage,
            cancellationToken: cancellationToken
        );
        
        try
        {
            // Получаем текст отчёта (утренний или вечерний)
            var reportText = answerFor.EveningReport
                ? report.EveningReport?.Data
                : report.MorningReport.Data;

            // Формируем текст сообщения
            var text = report.UserId == answerTo.Id
                ? string.Format(YourReportMessageFormat, reportText)
                : string.Format(
                    ReportMessageFormat,
                    request.User.FullName,
                    request.User.Position,
                    reportText);
            
            // Отправляем автору оригинальный отчёт
            var message = await client.SendMessage(
                chatId: answerTo.Id,
                text: text,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Отправляем автору ответ
            await client.SendMessage(
                replyParameters:new ReplyParameters
                {
                    MessageId = message.Id
                },
                chatId: answerTo.Id,
                text: string.Format(
                    AnswerMessageFormat,
                    answerFor.Message,
                    request.User.FullName,
                    request.User.Position,
                    request.Message),
                parseMode: ParseMode.Html,
                replyMarkup: DefaultKeyboard.AnswerKeyboard(
                    answerFor.ReportId, 
                    request.User.Id, 
                    answerFor.EveningReport),
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException ex)
        {
            // Логируем ошибку отправки сообщения
            logger.LogWarning(ex, "Failed to send answer notification to user {UserId}", answerTo.Id);
        }
    }

    /// <summary>
    /// Обновляет состояние пользователя и отправляет информационное сообщение
    /// </summary>
    /// <param name="request">Команда с данными запроса</param>
    /// <param name="newState">Новое состояние пользователя</param>
    /// <param name="message">Текст сообщения для отправки</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    private async Task UpdateStateAndSendMessageAsync(AnswerCommand request, State newState, string message, CancellationToken cancellationToken)
    {
        // Обновляем состояние пользователя (например, возвращаем в Idle или AwaitingReportInput)
        request.User!.State = newState;
    
        // Сбрасываем привязку к сообщению, на которое отвечали
        request.User.AnswerFor = null;
    
        // Фиксируем изменения состояния в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем пользователю информационное сообщение (ошибка/уведомление/подтверждение)
        await client.SendMessage(
            chatId: request.ChatId,
            text: message,
            parseMode: ParseMode.Html,
            cancellationToken: CancellationToken.None
        );
    }
}