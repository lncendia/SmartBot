using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Interfaces.Utils;
using SmartBot.Abstractions.Models.Reports;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для добавления комментария к отчёту.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="logger">Логгер для записи событий.</param>
public class AddCommentCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<AddCommentCommandHandler> logger)
    : IRequestHandler<AddCommentCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если отчёт не найден.
    /// </summary>
    private const string ReportNotFoundMessage =
        "<b>❌ Ошибка:</b> Отчёт не найден. Возможно, он был удалён.";

    /// <summary>
    /// Сообщение, которое отправляется, если комментарий пустой.
    /// </summary>
    private const string EmptyCommentMessage =
        "<b>❌ Ошибка:</b> Комментарий не может быть пустым. Пожалуйста, введите текст комментария.";

    /// <summary>
    /// Сообщение, которое отправляется, если суммарная длина комментария превышает 2000 символов.
    /// </summary>
    private const string CommentTooLongMessage =
        "<b>❌ Ошибка:</b> Суммарная длина комментария превышает 2000 символов. Пожалуйста, сократите текст.";

    /// <summary>
    /// Сообщение об успешном добавлении комментария.
    /// </summary>
    private const string CommentAddedSuccessMessage =
        "<b>✅ Комментарий успешно добавлен!</b>\n\n" +
        "Теперь вы можете продолжить работу с другими отчётами.";

    /// <summary>
    /// Сообщение, которое отправляется, если отчёт уже выгружен и комментарий не может быть добавлен.
    /// </summary>
    private const string ReportAlreadyExportedMessage =
        "<b>⚠️ Информация:</b> Отчёт уже выгружен.\n\n" +
        "Комментарий не может быть добавлен к выгруженному отчёту.";

    /// <summary>
    /// Формат сообщения с отчётом пользователя
    /// </summary>
    private const string ReportMessageFormat =
        "📝 <b>Новый комментарий на Ваш отчёт</b>\n\n" +
        "👇 <b>Текст отчёта:</b>\n" +
        "<blockquote>{0}</blockquote>\n\n";

    /// <summary>
    /// Формат сообщения с комментарием к отчёту
    /// </summary>
    private const string CommentMessageFormat =
        "<b>💬 Администратор {0}</b> ({1}):\n" +
        "<blockquote>{2}</blockquote>";

    /// <summary>
    /// Обрабатывает команду добавления комментария к отчёту.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, что комментарий не пустой
        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            // Отправляем сообщение о том, что комментарий пустой
            await client.SendMessage(
                chatId: request.ChatId,
                text: EmptyCommentMessage,
                parseMode: ParseMode.Html,
                replyMarkup: DefaultKeyboard.CancelKeyboard,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Определяем новое состояние пользователя
        var newState = request.User!.Role == Role.TeleAdmin
            ? State.AwaitingReportInput
            : State.Idle;

        // Проверяем существование отчёта
        if (request.User.ReviewingReport == null)
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

        // Получаем отчёт, который проверяет пользователь
        var report = await unitOfWork.Query<Report>()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == request.User!.ReviewingReport.ReportId, cancellationToken);

        // Если отчёт не найден
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

        // Если это не сегодняшний отчёт
        if (report.Date.Date != dateTimeProvider.Now.Date)
        {
            // Обновляем состояние пользователя и уведомляем о том, что это не сегодняшний отчёт
            await UpdateStateAndSendMessageAsync(
                request: request,
                newState: newState,
                message: ReportAlreadyExportedMessage,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Формируем новый комментарий
        var newComment = $"— {request.User.FullName} ({request.User.Position}): {request.Comment}";

        // Если у отчёта уже есть комментарий, добавляем два пропуска строки и новый комментарий
        if (!string.IsNullOrEmpty(report.Comment))
        {
            newComment = $"{report.Comment}\n\n{newComment}";
        }

        // Проверяем, не превышает ли длина комментария 2000 символов
        if (newComment.Length > 2000)
        {
            // Отправляем сообщение о превышении длины комментария
            await client.SendMessage(
                chatId: request.ChatId,
                text: CommentTooLongMessage,
                parseMode: ParseMode.Html,
                replyMarkup: DefaultKeyboard.CancelKeyboard,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Обновляем комментарий отчёта
        report.Comment = newComment;

        // Запоминаем данные сообщения, на которое пользователь отвечает для дальнейшей отправки ответного сообщения
        var reviewingReport = request.User!.ReviewingReport;

        // Обновляем состояние пользователя и уведомляем об успешном добавлении комментария
        await UpdateStateAndSendMessageAsync(
            request: request,
            newState: newState,
            message: CommentAddedSuccessMessage,
            cancellationToken: cancellationToken
        );

        // Получаем текст отчёта (утренний или вечерний)
        var reportText = reviewingReport.EveningReport
            ? report.EveningReport?.Data
            : report.MorningReport.Data;

        try
        {
            // Отправляем автору оригинальный отчёт
            var message = await client.SendMessage(
                text: string.Format(ReportMessageFormat, reportText),
                chatId: report.UserId,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Отправляем автору комментарий
            await client.SendMessage(
                replyParameters: new ReplyParameters
                {
                    MessageId = message.Id
                },
                text: string.Format(
                    CommentMessageFormat,
                    request.User.FullName,
                    request.User.Position,
                    request.Comment),
                replyMarkup: DefaultKeyboard.AnswerKeyboard(
                    reviewingReport.ReportId,
                    request.User.Id,
                    reviewingReport.EveningReport),
                chatId: report.UserId,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException ex)
        {
            // Логируем ошибку отправки сообщения
            logger.LogWarning(ex, "Failed to send comment notification to user {UserId}", report.UserId);
        }
    }

    /// <summary>
    /// Обновляет состояние пользователя и отправляет информационное сообщение
    /// </summary>
    /// <param name="request">Команда с данными запроса</param>
    /// <param name="newState">Новое состояние пользователя</param>
    /// <param name="message">Текст сообщения для отправки</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    private async Task UpdateStateAndSendMessageAsync(AddCommentCommand request, State newState, string message,
        CancellationToken cancellationToken)
    {
        // Обновляем состояние пользователя (например, возвращаем в Idle или AwaitingReportInput)
        request.User!.State = newState;

        // Сбрасываем привязку к сообщению, на которое отвечали
        request.User.ReviewingReport = null;

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