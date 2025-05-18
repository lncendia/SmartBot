using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Extensions;
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
/// <param name="dateTimeProvider">Провайдер для работы с текущим временем.</param>
/// <param name="logger">Логгер.</param>
/// <param name="options">Настройки параллелизма для рассылки сообщений.</param>
/// <param name="synchronizationService">Сервис синхронизации пользователей.</param>
/// <param name="motivationalMessageService">Сервис отправки мотивации на основании текста отчёта.</param>
public class SendReportWithoutAnalysisCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ParallelOptions options,
    IUserSynchronizationService synchronizationService,
    IMotivationalMessageService motivationalMessageService,
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
        
        // Если отчёт просрочен, то отправляем его в чаты и отправляем мотивацию и похвалу
        if (report.EveningReport?.Overdue.HasValue ?? report.MorningReport.Overdue.HasValue)
        {
            // Уведомляем администраторов о новом отчёте:
            // - всем администраторам системы
            // - в рабочий чат пользователя (если указан)
            await NotifyAdminsAsync(request, report, reportText);

            // Если анализатор включен, отправляем дополнительные сообщения:
            // - утренняя мотивация и рекомендации
            // - вечерняя оценка и похвала
            await motivationalMessageService.SendMotivationalMessagesAsync(
                request.ChatId,
                request.ReportMessageId,
                report,
                request.User,
                ct
            );
        }

        // иначе отправляем уведомление админам о проверке {
        //
        // }
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
            // Проверяем просрочку отправки утреннего отчета
            var overdue = now.MorningReportOverdue();
            
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

                    // Устанавливаем просрочку отправки утреннего отчета
                    Overdue = overdue,
                    
                    // Автоматически считаем принятыми просроченные отчёты
                    ApprovedBySystem = overdue.HasValue
                }
            };

            // Добавляем новый отчет в контекст данных
            await unitOfWork.AddAsync(report, ct);
        }
        // Сценарий 2: Обновление существующего отчета
        else
        {
            // Проверяем просрочку отправки вечернего отчета
            var overdue = now.MorningReportOverdue();
            
            // Добавляем или обновляем вечерний отчет
            report.EveningReport = new UserReport
            {
                // Сохраняем текст отчета из запроса
                Data = reportText,

                // Устанавливаем просрочку отправки вечернего отчета
                Overdue = overdue,
                
                // Автоматически считаем принятыми просроченные отчёты
                ApprovedBySystem = overdue.HasValue
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

            await client.SendMessage(
                chatId: request.ChatId,
                text: string.Format(MorningOverdueMessage, report.MorningReport.Overdue.FormatTimeSpan()),
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );

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

            await client.SendMessage(
                chatId: request.ChatId,
                text: string.Format(EveningOverdueMessage, report.EveningReport.Overdue.FormatTimeSpan()),
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }
    }
}