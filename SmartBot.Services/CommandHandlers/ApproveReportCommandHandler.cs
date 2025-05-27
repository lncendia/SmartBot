using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Extensions;
using SmartBot.Abstractions.Interfaces.Notification;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Interfaces.Utils;
using SmartBot.Abstractions.Models.Reports;
using SmartBot.Services.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для отправки отчёта без анализа.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="dateTimeProvider">Провайдер для работы с текущим временем.</param>
/// <param name="notificationService">Сервис рассылки уведомлений.</param>
/// <param name="synchronizationService">Сервис синхронизации пользователей.</param>
/// <param name="motivationalMessageService">Сервис отправки мотивации на основании текста отчёта.</param>
public class ApproveReportCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IUserSynchronizationService synchronizationService,
    IMotivationalMessageService motivationalMessageService,
    INotificationService notificationService)
    : IRequestHandler<ApproveReportCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется пользователю после успешного анализа и сохранения утреннего отчёта.
    /// Содержит мотивационное сообщение и напоминание о следующем шаге.
    /// </summary>
    private const string MorningSuccessMessage =
        "<b>Ваш отчёт был проверен и принят! ✅</b>\n\n" +
        "Теперь вы готовы к продуктивному дню! Не забывайте следить за своими целями и задачами. " +
        "Вечерний отчёт можно будет отправить после 17:00, чтобы подвести итоги дня.";

    /// <summary>
    /// Сообщение, которое отправляется пользователю после успешного анализа и сохранения вечернего отчёта.
    /// Содержит благодарность за проделанную работу и пожелание хорошего отдыха.
    /// </summary>
    private const string EveningSuccessMessage =
        "<b>Ваш вечерний отчёт был проверен и принят! ✅</b>\n\n" +
        "Спасибо за ваш труд и усилия! Желаем вам приятного вечера и хорошего отдыха! 🌙";

    /// <summary>
    /// Шаблон сообщения о необходимости сдать вечерний отчёт.
    /// </summary>
    private const string EveningReportDueMessage =
        "🌇 <b>Внимание! Сейчас время для сдачи вечернего отчёта.</b>\n\n" +
        "Пожалуйста, отправьте ваш <b>вечерний отчёт</b> как можно скорее. " +
        "Это важно для подведения итогов дня и планирования завтрашних задач.\n\n" +
        "📝 <i>Не забудьте указать ключевые результаты и планы на завтра.</i>";

    /// <summary>
    /// Сообщение, которое отправляется, если отчёт не найден.
    /// </summary>
    private const string ReportNotFoundMessage = "❌ Отчёт не найден.";
    
    /// <summary>
    /// Сообщение, которое отправляется, если отчёт уже подтвержден и не может быть отклонен.
    /// </summary>
    private const string ReportAlreadyApprovedMessage = "⚠️ Отчёт уже был подтвержден.";

    /// <summary>
    /// Сообщение, которое отправляется, если отчёт успешно подтвержден.
    /// </summary>
    private const string ReportSuccessfullyApprovedMessage = "✅ Отчёт успешно подтвержден.";

    /// <summary>
    /// Обрабатывает команду анализа отчёта.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде анализа отчёта.</param>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task Handle(ApproveReportCommand request, CancellationToken ct)
    {
        // Синхронизируем пользователя
        await synchronizationService.SynchronizeAsync(request.TelegramUserId, ct);

        // Так как команда помечена атрибутом AsyncCommand, она выполняется в другом контексте.
        // Поэтому обновляем сущность User что бы она отслеживалась
        unitOfWork.Update(request.User!);

        try
        {
            // Обрабатываем команду
            await ProcessCommandAsync(request, ct);
        }
        finally
        {
            // Освобождаем синхронизацию
            synchronizationService.Release(request.TelegramUserId);
        }
    }

    /// <summary>
    /// Основной метод обработки команды анализа отчёта.
    /// Выполняет последовательную проверку и обработку входящего отчёта.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные отчёта и контекст выполнения</param>
    /// <param name="ct">Токен для отмены асинхронной операции</param>
    private async Task ProcessCommandAsync(ApproveReportCommand request, CancellationToken ct)
    {
        // Получаем отчёт, который проверяет пользователь
        var report = await unitOfWork.Query<Report>()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct);

        // Если отчёт не найден
        if (report == null || (request.EveningReport && report.EveningReport == null))
        {
            // Отправляем сообщение о том, что отчёт не найден
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: ReportNotFoundMessage,
                cancellationToken: ct
            );

            // Удаляем сообщение с командой
            await request.TryDeleteMessageAsync(client, ct);

            // Завершаем выполнение метода
            return;
        }

        // Если отчёт уже был принят
        if (report.GetReport(request.EveningReport)!.IsApproved)
        {
            // Отправляем сообщение о том, что отчёт не найден
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: ReportAlreadyApprovedMessage,
                cancellationToken: ct
            );

            // Удаляем сообщение с командой
            await request.TryDeleteMessageAsync(client, ct);

            // Завершаем выполнение метода
            return;
        }

        // Отмечаем отчёт принятым
        report.GetReport(request.EveningReport)!.Approved = true;
        
        // Фиксируем все изменения в базе данных
        await unitOfWork.SaveChangesAsync(ct);
        
        // Отправляем сообщение о том, что отчёт не найден
        await client.AnswerCallbackQuery(
            callbackQueryId: request.CallbackQueryId,
            text: ReportSuccessfullyApprovedMessage,
            cancellationToken: CancellationToken.None
        );

        // Удаляем сообщение с командой
        await request.TryDeleteMessageAsync(client, CancellationToken.None);

        // Отправляем пользователю сообщение об успешной отправке:
        // - разный текст для утреннего/вечернего отчёта
        // - уведомление о просрочке при необходимости
        var message = await SendSuccessMessageToUserAsync(request.EveningReport, report.UserId);

        // Уведомляем администраторов о новом отчёте:
        // - всем администраторам системы
        // - в рабочий чат пользователя (если указан)
        await notificationService.NotifyNewReportAsync(report, request.User!, CancellationToken.None);

        // Если анализатор включен, отправляем дополнительные сообщения:
        // - утренняя мотивация и рекомендации
        // - вечерняя оценка и похвала
        await motivationalMessageService.SendMotivationalMessagesAsync(
            report.UserId,
            message.Id,
            report,
            CancellationToken.None
        );
    }

    /// <summary>
    /// Отправляет сообщение об успешном сохранении отчёта пользователю.
    /// </summary>
    /// <param name="eveningReport">Флаг, является ли отчёт вечерним</param>
    /// <param name="chatId">Идентификатор чата с пользователем.</param>
    private async Task<Message> SendSuccessMessageToUserAsync(bool eveningReport, ChatId chatId)
    {
        // Если вечерний отчёт
        if (eveningReport)
        {
            // Отправляем сообщение об успешном сохранении вечернего отчёта
            return await client.SendMessage(
                chatId: chatId,
                text: EveningSuccessMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }

        // Отправляем сообщение об успешном сохранении утреннего отчёта
        var message = await client.SendMessage(
            chatId: chatId,
            text: MorningSuccessMessage,
            parseMode: ParseMode.Html,
            cancellationToken: CancellationToken.None
        );

        // Если сейчас время вечернего отчёта, напоминаем о нём
        if (dateTimeProvider.Now.IsEveningPeriod())
        {
            await client.SendMessage(
                chatId: chatId,
                text: EveningReportDueMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }

        // Возвращаем сообщение
        return message;
    }
}