using MediatR;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Extensions;
using SmartBot.Abstractions.Interfaces.Notification;
using SmartBot.Abstractions.Interfaces.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для отправки отчёта без анализа.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="dateTimeProvider">Провайдер для работы с текущим временем.</param>
/// <param name="notificationService">Сервис рассылки уведомлений.</param>
/// <param name="motivationalMessageService">Сервис отправки мотивации на основании текста отчёта.</param>
public class AutomaticApproveReportCommandHandler(
    ITelegramBotClient client,
    IDateTimeProvider dateTimeProvider,
    IMotivationalMessageService motivationalMessageService,
    INotificationService notificationService)
    : IRequestHandler<AutomaticApproveReportCommand>
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
    /// Обрабатывает команду анализа отчёта.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде анализа отчёта.</param>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task Handle(AutomaticApproveReportCommand request, CancellationToken ct)
    {
        // Отправляем пользователю сообщение об успешной отправке:
        // - разный текст для утреннего/вечернего отчёта
        // - уведомление о просрочке при необходимости
        var message = await SendSuccessMessageToUserAsync(request.EveningReport, request.Report.UserId);

        // Уведомляем администраторов о новом отчёте:
        // - всем администраторам системы
        // - в рабочий чат пользователя (если указан)
        await notificationService.NotifyNewReportAsync(request.Report, token: ct);

        // Если анализатор включен, отправляем дополнительные сообщения:
        // - утренняя мотивация и рекомендации
        // - вечерняя оценка и похвала
        await motivationalMessageService.SendMotivationalMessagesAsync(
            request.Report.UserId,
            message.Id,
            request.Report,
            request.Report.User!,
            ct
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