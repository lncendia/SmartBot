using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Extensions;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Models.Reports;
using SmartBot.Services.Extensions;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = SmartBot.Abstractions.Models.Users.User;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для отправки отчёта без анализа.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="dateTimeProvider">Провайдер для работы с текущим временем.</param>
/// <param name="logger">Логгер.</param>
/// <param name="options">Настройки параллелизма для рассылки сообщений.</param>
/// <param name="synchronizationService">Сервис синхронизации пользователей.</param>
public class SendReportWithoutAnalysisCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ParallelOptions options,
    IUserSynchronizationService synchronizationService,
    ILogger<AnalyzeReportCommandHandler> logger)
    : IRequestHandler<SendReportWithoutAnalysisCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется пользователю после успешного анализа и сохранения утреннего отчёта.
    /// Содержит мотивационное сообщение и напоминание о следующем шаге.
    /// </summary>
    private const string MorningSuccessMessage =
        "<b>Отличный утренний отчёт! ✅</b>\n\n" +
        "Теперь вы готовы к продуктивному дню! Не забывайте следить за своими целями и задачами. " +
        "Вечерний отчёт можно будет отправить после 18:00, чтобы подвести итоги дня.";

    /// <summary>
    /// Сообщение, которое отправляется пользователю после успешного анализа и сохранения вечернего отчёта.
    /// Содержит благодарность за проделанную работу и пожелание хорошего отдыха.
    /// </summary>
    private const string EveningSuccessMessage =
        "<b>Отличный вечерний отчёт! ✅</b>\n\n" +
        "Спасибо за ваш труд и усилия! Желаем вам приятного вечера и хорошего отдыха! 🌙";

    /// <summary>
    /// Сообщение об ошибке, если отчёт пустой или превышает допустимую длину.
    /// </summary>
    private const string EmptyReportErrorMessage = "❌ Этот отчёт уже был отправлен.";

    /// <summary>
    /// Сообщение, если утренний отчёт уже был отправлен.
    /// Информирует пользователя о том, что утренний отчёт уже зарегистрирован.
    /// </summary>
    private const string MorningReportAlreadySentMessage = "⚠️ Утренний отчёт уже был отправлен.";

    /// <summary>
    /// Сообщение, если вечерний отчёт уже был отправлен.
    /// Информирует пользователя о том, что вечерний отчёт уже зарегистрирован.
    /// </summary>
    private const string EveningReportAlreadySentMessage = "⚠️ Вечерний отчёт уже был отправлен.";

    // Сообщение, информирующее о временных ограничениях для отправки отчётов.
    // Уведомляет пользователя, что в текущее время отправка отчётов невозможна.
    // Также указывает, что пользователь получит уведомление, когда наступит время для отправки отчёта.
    private const string ReportTimeRestrictionMessage = "⏰ Сейчас не время для отправки отчёта.";

    /// <summary>
    /// Сообщение для администратора о новом отчёте пользователя.
    /// Содержит имя пользователя и текст отчёта, а также приглашение оставить комментарий.
    /// </summary>
    private const string ReportSubmissionMessage =
        "📄 <b>Новый отчёт от пользователя</b> <i>{0}</i>\n" +
        "🧑‍🏭 <b>Должность:</b> <i>{1}</i>\n\n" +
        "👇 <b>Текст отчёта:</b>\n" +
        "<blockquote>{2}</blockquote>\n\n" +
        "📝 <i>Нажмите на кнопку, если хотите указать замечания или рекомендации для улучшения.</i>";

    /// <summary>
    /// Шаблон сообщения о просрочке утреннего отчёта.
    /// </summary>
    private const string MorningOverdueMessage =
        "⚠️ Вы просрочили утренний отчёт на {0}. Постарайтесь не задерживать отчёты в будущем!";

    /// <summary>
    /// Шаблон сообщения о просрочке вечернего отчёта.
    /// </summary>
    private const string EveningOverdueMessage =
        "⚠️ Вы просрочили вечерний отчёт на {0}. Постарайтесь не задерживать отчёты в будущем!";

    /// <summary>
    /// Шаблон сообщения о необходимости сдать вечерний отчёт.
    /// </summary>
    private const string EveningReportDueMessage =
        "🌇 <b>Внимание! Сейчас время для сдачи вечернего отчёта.</b>\n\n" +
        "Пожалуйста, отправьте ваш <b>вечерний отчёт</b> как можно скорее. " +
        "Это важно для подведения итогов дня и планирования завтрашних задач.\n\n" +
        "📝 <i>Не забудьте указать ключевые результаты и планы на завтра.</i>";

    /// <summary>
    /// Обрабатывает команду отправки отчёта без анализа.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде отправки отчёта без анализа.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(SendReportWithoutAnalysisCommand request, CancellationToken cancellationToken)
    {
        // Синхронизируем пользователя
        await synchronizationService.SynchronizeAsync(request.TelegramUserId, cancellationToken);

        try
        {
            // Обрабатываем команду
            await ProcessCommandAsync(request, cancellationToken);
        }
        finally
        {
            // Освобождаем синхронизацию
            synchronizationService.Release(request.TelegramUserId);
        }
    }

    /// <summary>
    /// Обрабатывает команду отправки отчёта без анализа.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде отправки отчёта без анализа.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task ProcessCommandAsync(SendReportWithoutAnalysisCommand request,
        CancellationToken cancellationToken)
    {
        // Если у пользователя нет текущего введенного отчёта
        if (string.IsNullOrWhiteSpace(request.User!.CurrentReport))
        {
            // Уведомляем пользователя о том, что нет текущего отчёта
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: EmptyReportErrorMessage,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Записываем текст отчёта в переменную
        var reportText = request.User.CurrentReport!;

        // Получаем текущую дату и время
        var now = dateTimeProvider.Now;

        // Если текущее время не подходит для отправки утреннего или вечернего отчёта
        if (!now.IsWorkingPeriod())
        {
            // Отправляем сообщение о временных ограничениях
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: ReportTimeRestrictionMessage,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Получаем отчёт из базы данных за текущую дату для указанного пользователя
        var report = await unitOfWork.Query<Report>()
            .Where(r => r.UserId == request.TelegramUserId)
            .Where(r => r.Date.Date == now.Date)
            .FirstOrDefaultAsync(cancellationToken);

        // Если вечерний отчёт уже сдан - ничего не делаем
        if (report?.EveningReport != null)
        {
            // Отправляем сообщение о том, что вечерний отчёт уже был отправлен
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: EveningReportAlreadySentMessage,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Если утренний отчёт уже сдан и еще не время для сдачи вечернего отчёта
        if (report?.MorningReport != null && !now.IsEveningPeriod())
        {
            // Отправляем сообщение о том, что утренний отчёт уже был отправлен
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: MorningReportAlreadySentMessage,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Получаем текущее время после анализа данных.
        var timeAfterAnalyze = dateTimeProvider.Now;

        // Если отчёт за текущую дату отсутствует, создаём новый.
        if (report == null)
        {
            // Создаём новый отчёт с текущей датой и ID пользователя.
            report = new Report
            {
                // Указываем ID пользователя, которому принадлежит отчёт.
                UserId = request.User!.Id,

                // Устанавливаем текущую дату для отчёта.
                Date = timeAfterAnalyze,

                // Заполняем данные утреннего отчёта.
                MorningReport = new UserReport
                {
                    // Данные отчёта, переданные в запросе.
                    Data = reportText,

                    // Проверяем, просрочен ли утренний отчёт.
                    Overdue = timeAfterAnalyze.MorningReportOverdue()
                }
            };

            // Добавляем отчёт в базу данных.
            await unitOfWork.AddAsync(report, cancellationToken);
        }
        else
        {
            // Если отчёт за текущую дату уже существует, обновляем вечерний отчёт.
            report.EveningReport = new UserReport
            {
                // Данные отчёта, переданные в запросе.
                Data = reportText,

                // Проверяем, просрочен ли вечерний отчёт.
                Overdue = timeAfterAnalyze.EveningReportOverdue()
            };
        }

        // Обнуляем состояние текущего введенного отчёта
        request.User.CurrentReport = null;

        // Сохраняем изменения в базе данных.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Удаляем сообщение с командой
        await request.TryDeleteMessageAsync(client, cancellationToken);

        // Определяем сообщение об успешном сохранении в зависимости от времени.
        if (report.EveningReport == null)
        {
            // Отправляем сообщение об успешном сохранении утреннего отчёта.
            await client.SendMessage(
                replyParameters: new ReplyParameters
                {
                    MessageId = request.ReportMessageId
                },
                chatId: request.ChatId,
                text: MorningSuccessMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );

            // Если утренний отчёт был просрочен, отправляем дополнительное уведомление.
            if (report.MorningReport.Overdue.HasValue)
            {
                // Формируем сообщение о просрочке.
                var overdueMessage =
                    string.Format(MorningOverdueMessage, report.MorningReport.Overdue.FormatTimeSpan());

                // Отправляем сообщение о просрочке.
                await client.SendMessage(
                    chatId: request.ChatId,
                    text: overdueMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: CancellationToken.None
                );
            }

            // Если сейчас время для сдачи вечернего отчёта
            if (timeAfterAnalyze.IsEveningPeriod())
            {
                // Отправляем сообщение о необходимости сдать вечерний отчёт.
                await client.SendMessage(
                    chatId: request.ChatId,
                    text: EveningReportDueMessage, // Используем константу с текстом сообщения
                    parseMode: ParseMode.Html,
                    cancellationToken: CancellationToken.None
                );
            }
        }
        else
        {
            // Отправляем сообщение об успешном сохранении вечернего отчёта.
            await client.SendMessage(
                replyParameters: new ReplyParameters
                {
                    MessageId = request.ReportMessageId
                },
                chatId: request.ChatId,
                text: EveningSuccessMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );

            // Если вечерний отчёт был просрочен, отправляем дополнительное уведомление.
            if (report.EveningReport.Overdue.HasValue)
            {
                // Формируем сообщение о просрочке.
                var overdueMessage =
                    string.Format(EveningOverdueMessage, report.EveningReport.Overdue.FormatTimeSpan());

                // Отправляем сообщение о просрочке.
                await client.SendMessage(
                    chatId: request.ChatId,
                    text: overdueMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: CancellationToken.None
                );
            }
        }

        // Получаем список ID администраторов
        var admins = await unitOfWork
            .Query<User>()
            .Where(u => u.Role == Role.Admin || u.Role == Role.TeleAdmin)
            .Select(u => u.Id)
            .ToListAsync(CancellationToken.None);

        // Создаем список из чатов, которые необходимо уведомить о новом отчёте
        var chatsToNotify = admins
            .Where(a => a != request.User.Id)
            .Select(a => new ValueTuple<long, int?>(a, null))
            .ToList();

        // Если у пользователя установлен рабочий чат - добавляем его в список чатов рассылки
        if (request.User!.WorkingChat != null)
        {
            // Сохраняем рабочий чат в переменную
            var workingChat = request.User.WorkingChat;

            // Создаем новый кортеж
            var tuple = new ValueTuple<long, int?>(workingChat.Id, workingChat.MessageThreadId);

            // Добавляем кортеж в список
            chatsToNotify.Add(tuple);
        }

        // Параллельно отправляем уведомления каждому администратору
        await Parallel.ForEachAsync(chatsToNotify, options, async (chat, ct) =>
        {
            try
            {
                // Отправляем сообщение администратору.
                await client.SendMessage(
                    chatId: chat.Item1,
                    messageThreadId: chat.Item2,
                    text: string.Format(
                        ReportSubmissionMessage,
                        request.User?.FullName,
                        request.User?.Position,
                        reportText),
                    parseMode: ParseMode.Html,
                    replyMarkup: AdminKeyboard.ExamReportKeyboard(report.Id, report.EveningReport != null),
                    cancellationToken: ct
                );
            }
            catch (ApiRequestException ex) // Ловим только ошибки Telegram API
            {
                // Логируем ошибку, если не удалось отправить сообщение.
                logger.LogWarning(ex, "Failed to send report submission notification to chat {ChatId}.", chat.Item1);
            }
        });
    }
}