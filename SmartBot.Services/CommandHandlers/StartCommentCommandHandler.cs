using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Models;
using SmartBot.Services.Keyboards.ExaminerKeyboard;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для начала ввода комментария к отчёту.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="dateTimeProvider">Провайдер для работы с текущим временем.</param>
/// <param name="logger">Логгер.</param>
public class StartCommentCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<StartCommentCommandHandler> logger)
    : IRequestHandler<StartCommentCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется пользователю, если он не является проверяющим.
    /// </summary>
    private const string NotExaminerMessage =
        "<b>❌ Ошибка:</b> Вы не являетесь проверяющим. Только проверяющие могут оставлять комментарии к отчётам.";

    /// <summary>
    /// Сообщение, которое отправляется, если отчёт не найден.
    /// </summary>
    private const string ReportNotFoundMessage =
        "<b>❌ Ошибка:</b> Отчёт не найден. Возможно, он был удалён или ещё не создан.";

    /// <summary>
    /// Сообщение, которое отправляется проверяющему для ввода комментария.
    /// </summary>
    private const string AwaitingCommentMessage =
        "<b>📝 Введите комментарий к отчёту:</b>\n\n" +
        "Пожалуйста, укажите ваши замечания или рекомендации для улучшения отчёта.";
    
    /// <summary>
    /// Сообщение, которое отправляется, если отчёт уже выгружен и комментарий не может быть добавлен.
    /// </summary>
    private const string ReportAlreadyExportedMessage =
        "<b>⚠️ Информация:</b> Отчёт уже выгружен.\n\n" +
        "Комментарий не может быть добавлен к выгруженному отчёту.";

    /// <summary>
    /// Обрабатывает команду начала ввода комментария к отчёту.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartCommentCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли пользователь проверяющим
        if (!request.User!.IsExaminer)
        {
            // Устанавливаем проверяющему состояние AwaitingReportInput
            request.User.State = State.AwaitingReportInput;

            // Сохраняем изменения в базе данных
            await unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                // Удаляем сообщение с командой
                await client.DeleteMessage(
                    chatId: request.ChatId,
                    messageId: request.MessageId,
                    cancellationToken: cancellationToken
                );
            }
            catch (ApiRequestException ex)
            {
                // Логируем ошибку, если не удалось удалить сообщение
                logger.LogWarning(ex, "The message with the ID {messageId} could not be deleted.", request.MessageId);
            }

            // Отправляем сообщение о том, что пользователь не является проверяющим
            await client.SendMessage(
                chatId: request.ChatId,
                text: NotExaminerMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Ищем отчёт в базе данных
        var report = await unitOfWork.Query<Report>()
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        // Если отчёт не найден
        if (report == null)
        {
            try
            {
                // Удаляем сообщение с командой
                await client.DeleteMessage(
                    chatId: request.ChatId,
                    messageId: request.MessageId,
                    cancellationToken: cancellationToken
                );
            }
            catch (ApiRequestException ex)
            {
                // Логируем ошибку, если не удалось удалить сообщение
                logger.LogWarning(ex, "The message with the ID {messageId} could not be deleted.", request.MessageId);
            }

            // Отправляем сообщение о том, что отчёт не найден
            await client.SendMessage(
                chatId: request.ChatId,
                text: ReportNotFoundMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }
        
        // Если это не сегодняшний отчёт
        if (report.Date.Date != dateTimeProvider.Now.Date)
        {
            try
            {
                // Удаляем сообщение с командой
                await client.DeleteMessage(
                    chatId: request.ChatId,
                    messageId: request.MessageId,
                    cancellationToken: cancellationToken
                );
            }
            catch (ApiRequestException ex)
            {
                // Логируем ошибку, если не удалось удалить сообщение
                logger.LogWarning(ex, "The message with the ID {messageId} could not be deleted.", request.MessageId);
            }

            // Отправляем сообщение о том, что это не сегодняшний отчёт
            await client.SendMessage(
                chatId: request.ChatId,
                text: ReportAlreadyExportedMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Устанавливаем у пользователя свойство ReviewingReportId
        request.User.ReviewingReportId = request.ReportId;

        // Устанавливаем состояние пользователя на AwaitingCommentInput
        request.User.State = State.AwaitingCommentInput;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение с запросом на ввод комментария
        await client.SendMessage(
            chatId: request.ChatId,
            text: AwaitingCommentMessage,
            parseMode: ParseMode.Html,
            replyMarkup: ExamKeyboard.GoBackKeyboard,
            cancellationToken: cancellationToken
        );
    }
}