using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Configuration;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Extensions;
using SmartBot.Abstractions.Interfaces.ReportAnalyzer;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Interfaces.Utils;
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
/// <param name="reportAnalyzer">Анализатор отчётов.</param>
/// <param name="dateTimeProvider">Провайдер для работы с текущим временем.</param>
/// <param name="logger">Логгер.</param>
/// <param name="options">Настройки параллелизма для рассылки сообщений.</param>
/// <param name="analyzerConfiguration">Конфигурация анализатора отчётов.</param>
/// <param name="synchronizationService">Сервис синхронизации пользователей.</param>
public class SendReportWithoutAnalysisCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    IReportAnalyzer reportAnalyzer,
    IDateTimeProvider dateTimeProvider,
    ParallelOptions options,
    ReportAnalysisConfiguration analyzerConfiguration,
    IUserSynchronizationService synchronizationService,
    ILogger<SendReportWithoutAnalysisCommandHandler> logger)
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
    /// Шаблон сообщения о прогрессе пользователя
    /// </summary>
    private const string RankProgressMessage =
        "<b>🏆 Ваш прогресс:</b>\n\n" +
        "{0}\n" +
        "📊 <b>Текущий рейтинг:</b> {1:N2} очков\n" +
        "🎯 <b>До следующего звания:</b> {2:N2} очков\n" +
        "👥 <b>Пользователей позади вас:</b> {3}";

    /// <summary>
    /// Обрабатывает команду анализа отчёта.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде анализа отчёта.</param>
    /// <param name="ct">Токен отмены операции.</param>
    public async Task Handle(SendReportWithoutAnalysisCommand request, CancellationToken ct)
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
    private async Task ProcessCommandAsync(SendReportWithoutAnalysisCommand request, CancellationToken ct)
    {
        // Первичная валидация содержимого отчёта:
        // - проверка на null/пустую строку
        // - проверка максимальной длины
        if (!await ValidateReportAsync(request, ct)) return;

        // Получаем текущее время с поправкой на часовой пояс
        var now = dateTimeProvider.Now;

        // Проверяем временные ограничения:
        // - рабочие часы для отправки отчётов
        // - корректное время для утреннего/вечернего отчёта
        if (!await CheckTimeRestrictionsAsync(request, now, ct)) return;

        // Получаем существующий отчёт из БД или создаём новый объект
        var report = await GetReportAsync(request, now, ct);

        // Проверяем возможность отправки отчёта:
        // - не был ли уже отправлен утренний/вечерний отчёт
        if (!await CheckReportAbilityAsync(request, report, now, ct)) return;

        // Записываем текущий отчёт в отдельную переменную
        var reportText = request.User!.CurrentReport!;
        
        // Обновляем данные отчёта и сохраняем в БД:
        // - для нового отчёта заполняем все поля
        // - для существующего обновляем вечерний отчёт
        report = await UpdateAndSaveReportAsync(request, report, now, reportText, ct);

        // Удаляем сообщение с командой
        await request.TryDeleteMessageAsync(client, ct);

        // Отправляем пользователю сообщение об успешной отправке:
        // - разный текст для утреннего/вечернего отчёта
        // - уведомление о просрочке при необходимости
        await SendSuccessMessageToUserAsync(request, report);

        // Уведомляем администраторов о новом отчёте:
        // - всем администраторам системы
        // - в рабочий чат пользователя (если указан)
        await NotifyAdminsAsync(request, report, reportText);

        // Если анализатор включен, отправляем дополнительные сообщения:
        // - утренняя мотивация и рекомендации
        // - вечерняя оценка и похвала
        await SendMotivationalMessagesAsync(request, report, ct);
    }

    /// <summary>
    /// Проверяет валидность отчёта и отправляет сообщение об ошибке, если отчёт невалиден.
    /// Выполняет две основные проверки:
    /// 1. Что отчёт не является пустым или состоящим только из пробелов
    /// 2. Что длина отчёта не превышает максимально допустимую (5000 символов)
    /// </summary>
    /// <param name="request">Запрос, содержащий данные отчёта и контекст сообщения</param>
    /// <param name="ct">Токен для отмены асинхронной операции</param>
    /// <returns>
    /// Возвращает true, если отчёт прошёл все проверки,
    /// false - если обнаружены нарушения валидации
    /// </returns>
    private async Task<bool> ValidateReportAsync(SendReportWithoutAnalysisCommand request, CancellationToken ct)
    {
        // Если у пользователя есть текущий введенный отчёт
        if (!string.IsNullOrWhiteSpace(request.User!.CurrentReport)) return true;

        // Уведомляем пользователя о том, что нет текущего отчёта
        await client.AnswerCallbackQuery(
            callbackQueryId: request.CallbackQueryId,
            text: EmptyReportErrorMessage,
            cancellationToken: ct
        );

        // Возвращаем false как индикатор невалидного отчёта
        return false;
    }

    /// <summary>
    /// Проверяет временные ограничения для отправки отчётов.
    /// Определяет, разрешено ли отправлять отчёты в текущий момент времени.
    /// </summary>
    /// <param name="request">Запрос с контекстом выполнения</param>
    /// <param name="now">Текущее время с учётом поправки часового пояса</param>
    /// <param name="ct">Токен для отмены операции</param>
    /// <returns>
    /// Возвращает true, если текущее время подходит для отправки отчётов,
    /// false - если отправка запрещена по временным ограничениям
    /// </returns>
    private async Task<bool> CheckTimeRestrictionsAsync(SendReportWithoutAnalysisCommand request, DateTime now,
        CancellationToken ct)
    {
        // Проверяем находится ли текущее время в разрешённом периоде
        if (now.IsWorkingPeriod()) return true;

        // Если время неподходящее, отправляем сообщение о временных ограничениях:
        // Отправляем сообщение о временных ограничениях
        await client.AnswerCallbackQuery(
            callbackQueryId: request.CallbackQueryId,
            text: ReportTimeRestrictionMessage,
            cancellationToken: ct
        );

        // Возвращаем false как индикатор временного ограничения
        return false;
    }

    /// <summary>
    /// Получает или создаёт отчёт из базы данных.
    /// </summary>
    /// <param name="request">Запрос с данными отчёта.</param>
    /// <param name="now">Текущее время.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Объект отчёта или null, если дальнейшая обработка не требуется.</returns>
    private async Task<Report?> GetReportAsync(SendReportWithoutAnalysisCommand request, DateTime now,
        CancellationToken ct)
    {
        // Запрашиваем отчёт из базы данных по ID пользователя и дате
        return await unitOfWork.Query<Report>()
            .Where(r => r.UserId == request.TelegramUserId)
            .Where(r => r.Date.Date == now.Date)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Проверяет возможность отправки отчёта в текущий момент времени.
    /// Выполняет две основные проверки:
    /// 1. Проверяет не был ли уже отправлен вечерний отчёт
    /// 2. Проверяет не был ли уже отправлен утренний отчёт, когда ещё не наступило время вечернего
    /// </summary>
    /// <param name="request">Запрос с данными отчёта и контекстом сообщения</param>
    /// <param name="report">Объект отчёта из базы данных (может быть null если отчёт новый)</param>
    /// <param name="now">Текущее время с учётом временной зоны</param>
    /// <param name="ct">Токен для отмены асинхронных операций</param>
    /// <returns>
    /// Возвращает true если:
    /// - отчёт новый (null)
    /// - вечерний отчёт ещё не отправлен
    /// - утренний отчёт не отправлен или наступило время вечернего.
    /// Возвращает false и отправляет соответствующее сообщение если:
    /// - вечерний отчёт уже существует
    /// - утренний отчёт уже существует и не наступило время вечернего
    /// </returns>
    private async Task<bool> CheckReportAbilityAsync(SendReportWithoutAnalysisCommand request, Report? report,
        DateTime now,
        CancellationToken ct)
    {
        // Проверка 1: Вечерний отчёт уже существует
        if (report?.EveningReport != null)
        {
            // Отправляем сообщение о том, что вечерний отчёт уже был отправлен
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: EveningReportAlreadySentMessage,
                cancellationToken: ct
            );

            // Возвращаем false - отправка невозможна
            return false;
        }

        // Проверка 2: Утренний отчёт существует И сейчас не вечерний период
        if (report?.MorningReport != null && !now.IsEveningPeriod())
        {
            // Отправляем сообщение о том, что утренний отчёт уже был отправлен
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: MorningReportAlreadySentMessage,
                cancellationToken: ct
            );

            // Возвращаем false - отправка невозможна
            return false;
        }

        // Если все проверки пройдены - возвращаем true
        return true;
    }

    /// <summary>
    /// Обновляет или создает новый отчет и сохраняет его в базу данных.
    /// Обрабатывает два сценария:
    /// 1. Создание нового отчета (если report == null)
    /// 2. Обновление существующего отчета (добавление вечернего отчета)
    /// </summary>
    /// <param name="request">Запрос с данными отчета и контекстом пользователя</param>
    /// <param name="report">Существующий объект отчета или null</param>
    /// <param name="now">Текущее время с учетом часового пояса</param>
    /// <param name="reportText">Текст отчёта.</param>
    /// <param name="ct">Токен для отмены асинхронных операций</param>
    /// <returns>Обновленный или созданный объект отчета</returns>
    private async Task<Report> UpdateAndSaveReportAsync(SendReportWithoutAnalysisCommand request, Report? report,
        DateTime now,
        string reportText,
        CancellationToken ct)
    {
        // Сценарий 1: Создание нового отчета
        if (report == null)
        {
            // Инициализируем новый объект отчета
            report = new Report
            {
                // Устанавливаем связь с пользователем
                UserId = request.User!.Id,

                // Фиксируем текущую дату
                Date = now,

                // Создаем утренний отчет
                MorningReport = new UserReport
                {
                    // Сохраняем текст отчета из запроса
                    Data = reportText,

                    // Проверяем просрочку отправки утреннего отчета
                    Overdue = now.MorningReportOverdue()
                }
            };

            // Добавляем новый отчет в контекст данных
            await unitOfWork.AddAsync(report, ct);
        }
        // Сценарий 2: Обновление существующего отчета
        else
        {
            // Добавляем или обновляем вечерний отчет
            report.EveningReport = new UserReport
            {
                // Сохраняем текст отчета из запроса
                Data = reportText,

                // Проверяем просрочку отправки вечернего отчета
                Overdue = now.EveningReportOverdue()
            };
        }

        // Очищаем временное состояние текущего отчета пользователя
        request.User!.CurrentReport = null;

        // Фиксируем все изменения в базе данных
        await unitOfWork.SaveChangesAsync(ct);

        // Возвращаем обновленный/созданный объект отчета
        return report;
    }

    /// <summary>
    /// Отправляет уведомления о сохранении отчёта пользователю и администраторам.
    /// </summary>
    /// <param name="request">Запрос с данными отчёта.</param>
    /// <param name="report">Объект отчёта.</param>
    /// <param name="reportText">Текст отчёта.</param>
    private async Task NotifyAdminsAsync(SendReportWithoutAnalysisCommand request, Report report, string reportText)
    {
        // Получаем список администраторов
        var admins = await unitOfWork
            .Query<User>()
            .Where(u => u.Role == Role.Admin || u.Role == Role.TeleAdmin)
            .Select(u => u.Id)
            .ToListAsync(CancellationToken.None);

        // Формируем список чатов для уведомлений
        var chatsToNotify = admins
            .Where(a => a != request.User!.Id)
            .Select(a => new ValueTuple<long, int?>(a, null))
            .ToList();

        // Добавляем рабочий чат пользователя, если он есть
        if (request.User!.WorkingChat != null)
        {
            chatsToNotify.Add(new ValueTuple<long, int?>(
                request.User.WorkingChat.Id,
                request.User.WorkingChat.MessageThreadId));
        }

        // Параллельно отправляем уведомления администраторам
        await Parallel.ForEachAsync(chatsToNotify, options, async (chat, ct) =>
        {
            try
            {
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
            catch (ApiRequestException ex)
            {
                logger.LogWarning(ex, "Failed to send report submission notification to chat {ChatId}.", chat.Item1);
            }
        });
    }

    /// <summary>
    /// Отправляет сообщение об успешном сохранении отчёта пользователю.
    /// </summary>
    /// <param name="request">Запрос с данными отчёта.</param>
    /// <param name="report">Объект отчёта.</param>
    private async Task SendSuccessMessageToUserAsync(SendReportWithoutAnalysisCommand request, Report report)
    {
        if (report.EveningReport == null)
        {
            // Отправляем сообщение об успешном сохранении утреннего отчёта
            await client.SendMessage(
                replyParameters: new ReplyParameters { MessageId = request.ReportMessageId },
                chatId: request.ChatId,
                text: MorningSuccessMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );

            // Если утренний отчёт просрочен, отправляем уведомление
            if (report.MorningReport.Overdue.HasValue)
            {
                await client.SendMessage(
                    chatId: request.ChatId,
                    text: string.Format(MorningOverdueMessage, report.MorningReport.Overdue.FormatTimeSpan()),
                    parseMode: ParseMode.Html,
                    cancellationToken: CancellationToken.None
                );
            }

            // Если сейчас время вечернего отчёта, напоминаем о нём
            if (dateTimeProvider.Now.IsEveningPeriod())
            {
                await client.SendMessage(
                    chatId: request.ChatId,
                    text: EveningReportDueMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: CancellationToken.None
                );
            }
        }
        else
        {
            // Отправляем сообщение об успешном сохранении вечернего отчёта
            await client.SendMessage(
                replyParameters: new ReplyParameters { MessageId = request.ReportMessageId },
                chatId: request.ChatId,
                text: EveningSuccessMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );

            // Если вечерний отчёт просрочен, отправляем уведомление
            if (report.EveningReport.Overdue.HasValue)
            {
                await client.SendMessage(
                    chatId: request.ChatId,
                    text: string.Format(EveningOverdueMessage, report.EveningReport.Overdue.FormatTimeSpan()),
                    parseMode: ParseMode.Html,
                    cancellationToken: CancellationToken.None
                );
            }
        }
    }

    /// <summary>
    /// Отправляет мотивационные сообщения пользователю в зависимости от типа отчёта.
    /// Определяет тип отчёта (утренний/вечерний) и вызывает соответствующие обработчики.
    /// </summary>
    /// <param name="request">Запрос с данными отчёта.</param>
    /// <param name="report">Объект отчёта.</param>
    /// <param name="ct">Токен отмены для асинхронных операций</param>
    /// <returns>Task, представляющий асинхронную операцию отправки сообщений</returns>
    private async Task SendMotivationalMessagesAsync(SendReportWithoutAnalysisCommand request, Report report,
        CancellationToken ct)
    {
        // Проверяем включен ли модуль анализатора в конфигурации системы
        if (!analyzerConfiguration.Enabled) return;

        // Определяем тип отчёта по наличию вечернего отчёта
        if (report.EveningReport == null)
        {
            try
            {
                // Отправка мотивационного сообщения
                await SendMorningMotivationAsync(
                    request,
                    report.MorningReport.Data,
                    ct);
            }
            catch
            {
                // ignored
            }
        }
        else
        {
            try
            {
                // Отправка похвалы за проделанную работу
                await SendEveningPraiseAsync(
                    request,
                    report.EveningReport.Data,
                    ct);
            }
            catch
            {
                // ignored
            }

            try
            {
                // Обновление рейтинга пользователя
                await ProcessUserScoreAsync(
                    request,
                    report.EveningReport.Data,
                    ct);
            }
            catch
            {
                //ignored
            }
        }
    }

    /// <summary>
    /// Отправляет утренние мотивационные сообщения пользователю
    /// </summary>
    /// <param name="request">Контекст запроса</param>
    /// <param name="reportData">Текст утреннего отчета</param>
    /// <param name="ct">Токен отмены</param>
    private async Task SendMorningMotivationAsync(SendReportWithoutAnalysisCommand request, string reportData,
        CancellationToken ct)
    {
        // Генерируем мотивацию на основе утреннего отчета
        var motivation = reportAnalyzer.GenerateMorningMotivationAsync(reportData, ct);
        await SendTypingWhileWaitingAsync(request, motivation, ct);

        // Отправляем три типа сообщений последовательно:
        // 1. Основная мотивация
        await client.SendMessage(
            replyParameters: new ReplyParameters { MessageId = request.ReportMessageId },
            chatId: request.ChatId,
            text: motivation.Result.Motivation,
            cancellationToken: ct
        );

        // Отправляем индикатор "печатает" в чат пользователя.
        await client.SendChatAction(request.ChatId, ChatAction.Typing, cancellationToken: ct);

        // Создаем задачу задержки на 1 секунду с учетом токена отмены
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        // 2. Рекомендации на день
        await client.SendMessage(
            replyParameters: new ReplyParameters { MessageId = request.ReportMessageId },
            chatId: request.ChatId,
            text: motivation.Result.Recommendations,
            cancellationToken: ct
        );

        // Отправляем индикатор "печатает" в чат пользователя.
        await client.SendChatAction(request.ChatId, ChatAction.Typing, cancellationToken: ct);

        // Создаем задачу задержки на 1 секунду с учетом токена отмены
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        // 3. Юмористическое завершение
        await client.SendMessage(
            chatId: request.ChatId,
            text: motivation.Result.Humor,
            cancellationToken: ct
        );
    }

    /// <summary>
    /// Отправляет вечерние похвалу и оценку пользователю
    /// </summary>
    /// <param name="request">Контекст запроса</param>
    /// <param name="reportData">Текст утреннего отчета</param>
    /// <param name="ct">Токен отмены</param>
    private async Task SendEveningPraiseAsync(SendReportWithoutAnalysisCommand request, string reportData,
        CancellationToken ct)
    {
        // Генерируем похвалу на основе вечернего отчета
        var praise = reportAnalyzer.GenerateEveningPraiseAsync(reportData, CancellationToken.None);
        await SendTypingWhileWaitingAsync(request, praise, ct);

        // Отправляем три типа сообщений:
        // 1. Достижения за день
        await client.SendMessage(
            replyParameters: new ReplyParameters { MessageId = request.ReportMessageId },
            chatId: request.ChatId,
            text: praise.Result.Achievements,
            cancellationToken: ct
        );

        // Отправляем индикатор "печатает" в чат пользователя.
        await client.SendChatAction(request.ChatId, ChatAction.Typing, cancellationToken: ct);

        // Создаем задачу задержки на 1 секунду с учетом токена отмены
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        // 2. Похвалу за проделанную работу
        await client.SendMessage(
            replyParameters: new ReplyParameters { MessageId = request.ReportMessageId },
            chatId: request.ChatId,
            text: praise.Result.Praise,
            cancellationToken: ct
        );

        // Отправляем индикатор "печатает" в чат пользователя.
        await client.SendChatAction(request.ChatId, ChatAction.Typing, cancellationToken: ct);

        // Создаем задачу задержки на 1 секунду с учетом токена отмены
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        // 3. Юмористическое завершение дня
        await client.SendMessage(
            chatId: request.ChatId,
            text: praise.Result.Humor,
            cancellationToken: ct
        );
    }

    /// <summary>
    /// Обрабатывает начисление очков пользователю за вечерний отчет:
    /// 1. Получает оценку отчета
    /// 2. Начисляет очки
    /// 3. Проверяет изменение звания
    /// 4. Формирует отчет о прогрессе
    /// </summary>
    /// <param name="request">Данные запроса, включая пользователя и контекст чата</param>
    /// <param name="reportData">Текст вечернего отчета для анализа</param>
    /// <param name="ct">Токен отмены для асинхронных операций</param>
    private async Task ProcessUserScoreAsync(SendReportWithoutAnalysisCommand request, string reportData,
        CancellationToken ct)
    {
        // Запускаем асинхронную задачу для получения оценки отчета
        var scoreTask = reportAnalyzer.GetScorePointsAsync(reportData, ct);

        // Пока задача выполняется, периодически отправляем индикатор "печатает"
        await SendTypingWhileWaitingAsync(request, scoreTask, ct);

        // Получаем результат выполнения задачи - количество заработанных очков
        var earnedScore = scoreTask.Result;

        // Сохраняем текущее звание ДО начисления очков для последующего сравнения
        var previousRank = request.User!.Rank;

        // Начисляем очки, если они положительные
        if (earnedScore > 0) request.User.Score += earnedScore;

        // Сохраняем изменения
        await unitOfWork.SaveChangesAsync(ct);

        // Получаем актуальное звание ПОСЛЕ начисления очков
        var currentRank = request.User.Rank;

        // Вычисляем сколько очков осталось до следующего звания
        var pointsRemaining = request.User.PointsToNextRank;

        // Запрос в БД для подсчета количества пользователей с меньшим рейтингом
        var usersBehindCount = await unitOfWork.Query<User>()
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)
            .Where(u => u.Score < request.User.Score)
            .CountAsync(ct);

        // Формируем основное сообщение в зависимости от изменения звания
        var statusMessage = previousRank == currentRank
            ? $"📈 <b>Вы</b> ({currentRank}) стали ближе к новому званию!"
            : $"🎉 Поздравляем с повышением до <b>{currentRank}!</b>";

        // Формируем итоговое сообщение, подставляя данные в шаблон
        var message = string.Format(RankProgressMessage,
            statusMessage,
            request.User.Score,
            pointsRemaining,
            usersBehindCount);

        // Отправляем сформированное сообщение пользователю
        await client.SendMessage(
            chatId: request.ChatId,
            text: message,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }
    
    /// <summary>
    /// Отправляет статус "печатает" пока задача не завершена.
    /// </summary>
    /// <param name="request">Запрос с данными отчёта.</param>
    /// <param name="task">Задача, за которой нужно следить.</param>
    /// <param name="ct">Токен отмены операции.</param>
    private async Task SendTypingWhileWaitingAsync(SendReportWithoutAnalysisCommand request, Task task,
        CancellationToken ct)
    {
        while (!task.IsCompleted)
        {
            await client.SendChatAction(request.ChatId, ChatAction.Typing, cancellationToken: ct);
            await Task.Delay(5000, ct);
        }
    }
}