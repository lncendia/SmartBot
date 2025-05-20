using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces.Notification;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Interfaces.Utils;
using SmartBot.Abstractions.Models.Reports;
using SmartBot.Abstractions.Models.Users;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

// ReSharper disable InconsistentNaming

namespace SmartBot.Services.Services;

/// <summary>
/// Сервис для отправки уведомлений пользователям через Telegram.
/// </summary>
/// <param name="client">Клиент Telegram Bot API для отправки сообщений.</param>
/// <param name="unitOfWork">Единица работы для взаимодействия с базой данных.</param>
/// <param name="dateTimeProvider">Провайдер времени для работы с датами и временем.</param>
/// <param name="logger">Логгер для записи событий и ошибок.</param>
/// <param name="options">Настройки параллелизма для рассылки сообщений.</param>
public class NotificationService(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ParallelOptions options,
    ILogger<NotificationService> logger) : INotificationService
{
    /// <summary>
    /// Шаблон сообщения для уведомления о необходимости сдать утренний отчёт.
    /// </summary>
    private const string MorningReportMessage = "<b>Доброе утро! 🌞</b>\n\n" +
                                                "Не забудьте отправить ваш <b>утренний отчёт</b>. " +
                                                "Это поможет вам начать день с чёткого плана и продуктивности!";

    /// <summary>
    /// Шаблон сообщения для уведомления о том, что время сдачи утреннего отчёта подходит к концу.
    /// </summary>
    private const string MorningDeadlineApproachingMessage =
        "<b>Напоминание: Время сдачи утреннего отчёта истекает ⏰</b>\n\n" +
        "У вас осталось меньше часа, чтобы отправить ваш <b>утренний отчёт</b>. " +
        "Не упустите возможность начать день с ясными целями!";

    /// <summary>
    /// Шаблон сообщения для уведомления о том, что утренний отчёт не был сдан.
    /// </summary>
    private const string MorningReportMissedMessage = "<b>Внимание: Утренний отчёт не сдан ⚠️</b>\n\n" +
                                                      "Вы не отправили ваш <b>утренний отчёт</b> вовремя. " +
                                                      "Вы можете отправить его сейчас, но он будет отмечен как просроченный.";

    /// <summary>
    /// Шаблон сообщения для уведомления о необходимости сдать вечерний отчёт (если утренний отчёт сдан).
    /// </summary>
    private const string EveningReportMessage_MorningDone = "<b>Добрый вечер! 🌇</b>\n\n" +
                                                            "Пожалуйста, найдите время, чтобы отправить ваш <b>вечерний отчёт</b>. " +
                                                            "Это важно для подведения итогов дня и планирования завтрашних задач.";

    /// <summary>
    /// Шаблон сообщения для уведомления о необходимости сдать вечерний отчёт (если утренний отчёт не сдан).
    /// </summary>
    private const string EveningReportMessage_MorningMissed = "<b>Добрый вечер! 🌇</b>\n\n" +
                                                              "Сейчас время для <b>вечернего отчёта</b>, но вы ещё не отправили <b>утренний отчёт</b>. " +
                                                              "Пожалуйста, сначала отправьте пропущенный <b>утренний отчёт</b>, а затем переходите к вечернему.";

    /// <summary>
    /// Шаблон сообщения для уведомления о том, что время сдачи вечернего отчёта подходит к концу (если утренний отчёт сдан).
    /// </summary>
    private const string EveningDeadlineApproachingMessage_MorningDone =
        "<b>Напоминание: Время сдачи вечернего отчёта истекает ⏰</b>\n\n" +
        "У вас осталось меньше часа, чтобы отправить ваш <b>вечерний отчёт</b>. " +
        "Не забудьте подвести итоги дня!";

    /// <summary>
    /// Шаблон сообщения для уведомления о том, что время сдачи вечернего отчёта подходит к концу (если утренний отчёт не сдан).
    /// </summary>
    private const string EveningDeadlineApproachingMessage_MorningMissed =
        "<b>Напоминание: Время сдачи вечернего отчёта истекает ⏰</b>\n\n" +
        "У вас осталось меньше часа, чтобы отправить ваш <b>вечерний отчёт</b>. " +
        "Однако вы ещё не отправили <b>утренний отчёт</b>. " +
        "Пожалуйста, сначала отправьте пропущенный утренний отчёт, а затем переходите к вечернему.";

    /// <summary>
    /// Шаблон сообщения для уведомления о том, что вечерний отчёт не был сдан (если утренний отчёт сдан).
    /// </summary>
    private const string EveningReportMissedMessage_MorningDone = "<b>Внимание: Вечерний отчёт не сдан ⚠️</b>\n\n" +
                                                                  "Вы не отправили ваш <b>вечерний отчёт</b> вовремя. " +
                                                                  "Вы можете отправить его сейчас, но он будет отмечен как просроченный.";

    /// <summary>
    /// Шаблон сообщения для уведомления о том, что вечерний отчёт не был сдан. (Если утренний отчёт тоже не сдан).
    /// </summary>
    private const string EveningReportMissedMessage_MorningMissed = "<b>Внимание: Отчёты не сданы ⚠️</b>\n\n" +
                                                                    "Вы не отправили ваш <b>вечерний отчёт</b> вовремя. " +
                                                                    "Кроме того, вы ещё не отправили <b>утренний отчёт</b>. " +
                                                                    "Пожалуйста, сначала отправьте пропущенный утренний отчёт, а затем переходите к вечернему. " +
                                                                    "Оба отчёта можно отправить сейчас, но они будут отмечены как просроченные.";

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет пользователей о необходимости сдать утренний отчёт.
    /// </summary>
    public async Task NotifyMorningReportDueAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время.
        var now = dateTimeProvider.Now;

        // Находим пользователей, которые ещё не сдали утренний отчёт за текущий день.
        var users = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых нет отчётов за текущий день.
            .Where(u => u.Reports.All(r => r.Date.Date != now.Date))

            // Фильтруем пользователей, не являющихся администраторами
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Фильтруем пользователей, которые заполнили свои данные`
            .Where(u => u.Position != null)

            // Выбираем только ID пользователей.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Отправляем сообщения пользователям, которые не сдали утренний отчёт
        await SendMessagesAsync(users, MorningReportMessage, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет пользователей о том, что время сдачи утреннего отчёта подходит к концу.
    /// </summary>
    public async Task NotifyMorningReportDeadlineApproachingAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время.
        var now = dateTimeProvider.Now;

        // Находим пользователей, которые ещё не сдали утренний отчёт за текущий день.
        var users = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых нет отчётов за текущий день.
            .Where(u => u.Reports.All(r => r.Date.Date != now.Date))

            // Фильтруем пользователей, не являющихся администраторами
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Фильтруем пользователей, которые заполнили свои данные`
            .Where(u => u.Position != null)

            // Выбираем только ID пользователей.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Отправляем сообщения пользователям, которые не сдали утренний отчёт
        await SendMessagesAsync(users, MorningDeadlineApproachingMessage, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет пользователей о том, что утренний отчёт не был сдан.
    /// </summary>
    public async Task NotifyMorningReportMissedAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время.
        var now = dateTimeProvider.Now;

        // Находим пользователей, которые ещё не сдали утренний отчёт за текущий день.
        var users = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых нет отчётов за текущий день.
            .Where(u => u.Reports.All(r => r.Date.Date != now.Date))

            // Фильтруем пользователей, не являющихся администраторами
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Фильтруем пользователей, которые заполнили свои данные`
            .Where(u => u.Position != null)

            // Выбираем только ID пользователей.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Отправляем сообщения пользователям, которые не сдали утренний отчёт
        await SendMessagesAsync(users, MorningReportMissedMessage, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет пользователей о необходимости сдать вечерний отчёт.
    /// </summary>
    public async Task NotifyEveningReportDueAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время для проверки отчётов за сегодняшний день.
        var now = dateTimeProvider.Now;

        // Пользователи, которые сдали утренний отчёт, но не сдали вечерний.
        var usersWithMorningReport = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых есть утренний отчёт за сегодня, но нет вечернего.
            .Where(u => u.Reports.Any(r => r.Date.Date == now.Date && r.EveningReport == null))

            // Фильтруем пользователей, не являющихся администраторами
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Фильтруем пользователей, которые заполнили свои данные`
            .Where(u => u.Position != null)

            // Выбираем только ID пользователей для дальнейшей обработки.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Пользователи, которые не сдали ни утренний, ни вечерний отчёт.
        var usersWithoutMorningReport = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых либо вообще нет отчётов за сегодня
            .Where(u => u.Reports.All(r => r.Date.Date != now.Date))

            // Фильтруем пользователей, не являющихся администраторами
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Фильтруем пользователей, которые заполнили свои данные`
            .Where(u => u.Position != null)

            // Выбираем только ID пользователей для дальнейшей обработки.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Отправляем сообщения пользователям, которые сдали утренний отчёт.
        await SendMessagesAsync(usersWithMorningReport, EveningReportMessage_MorningDone,
            cancellationToken: cancellationToken);

        // Отправляем сообщения пользователям, которые не сдали утренний отчёт.
        await SendMessagesAsync(usersWithoutMorningReport, EveningReportMessage_MorningMissed,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет пользователей о том, что время сдачи вечернего отчёта подходит к концу.
    /// </summary>
    public async Task NotifyEveningReportDeadlineApproachingAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время для проверки отчётов за сегодняшний день.
        var now = dateTimeProvider.Now;

        // Пользователи, которые сдали утренний отчёт, но не сдали вечерний.
        var usersWithMorningReport = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых есть утренний отчёт за сегодня, но нет вечернего.
            .Where(u => u.Reports.Any(r => r.Date.Date == now.Date && r.EveningReport == null))

            // Фильтруем пользователей, не являющихся администраторами
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Фильтруем пользователей, которые заполнили свои данные`
            .Where(u => u.Position != null)

            // Выбираем только ID пользователей для дальнейшей обработки.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Пользователи, которые не сдали ни утренний, ни вечерний отчёт.
        var usersWithoutMorningReport = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых либо вообще нет отчётов за сегодня
            .Where(u => u.Reports.All(r => r.Date.Date != now.Date))

            // Фильтруем пользователей, не являющихся администраторами
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Фильтруем пользователей, которые заполнили свои данные`
            .Where(u => u.Position != null)

            // Выбираем только ID пользователей для дальнейшей обработки.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Отправляем сообщения пользователям, которые сдали утренний отчёт
        await SendMessagesAsync(usersWithMorningReport, EveningDeadlineApproachingMessage_MorningDone,
            cancellationToken: cancellationToken);

        // Отправляем сообщения пользователям, которые не сдали утренний отчёт
        await SendMessagesAsync(usersWithoutMorningReport, EveningDeadlineApproachingMessage_MorningMissed,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет пользователей о том, что вечерний отчёт не был сдан.
    /// </summary>
    public async Task NotifyEveningReportMissedAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время для проверки отчётов за сегодняшний день.
        var now = dateTimeProvider.Now;

        // Пользователи, которые сдали утренний отчёт, но не сдали вечерний.
        var usersWithMorningReport = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых есть утренний отчёт за сегодня, но нет вечернего.
            .Where(u => u.Reports.Any(r => r.Date.Date == now.Date && r.EveningReport == null))

            // Фильтруем пользователей, не являющихся администраторами
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Фильтруем пользователей, которые заполнили свои данные`
            .Where(u => u.Position != null)

            // Выбираем только ID пользователей для дальнейшей обработки.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Пользователи, которые не сдали ни утренний, ни вечерний отчёт.
        var usersWithoutMorningReport = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых либо вообще нет отчётов за сегодня
            .Where(u => u.Reports.All(r => r.Date.Date != now.Date))

            // Фильтруем пользователей, не являющихся администраторами
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Фильтруем пользователей, которые заполнили свои данные`
            .Where(u => u.Position != null)

            // Выбираем только ID пользователей для дальнейшей обработки.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Отправляем сообщения пользователям, которые сдали утренний отчёт
        await SendMessagesAsync(usersWithMorningReport, EveningReportMissedMessage_MorningDone,
            cancellationToken: cancellationToken);

        // Отправляем сообщения пользователям, которые не сдали утренний отчёт
        await SendMessagesAsync(usersWithoutMorningReport, EveningReportMissedMessage_MorningMissed,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Сообщение для администратора о новом отчёте пользователя.
    /// Содержит имя пользователя и текст отчёта, а также приглашение оставить комментарий.
    /// </summary>
    private const string ReportHandSubmissionMessage =
        "📄 <b>Новый отчёт от пользователя</b> <i>{0}</i>\n" +
        "🧑‍🏭 <b>Должность:</b> <i>{1}</i>\n\n" +
        "🧑‍🏭 <b>Проверил:</b> <i>{2} ({3})</i>\n\n" +
        "👇 <b>Текст отчёта:</b>\n" +
        "<blockquote>{4}</blockquote>\n\n" +
        "📝 <i>Нажмите на кнопку, если хотите указать замечания или рекомендации для улучшения.</i>";

    /// <summary>
    /// Сообщение для администратора о новом отчёте пользователя.
    /// Содержит имя пользователя и текст отчёта, а также приглашение оставить комментарий.
    /// </summary>
    private const string ReportSystemSubmissionMessage =
        "📄 <b>Новый отчёт от пользователя</b> <i>{0}</i>\n" +
        "🧑‍🏭 <b>Должность:</b> <i>{1}</i>\n\n" +
        "🧑‍🏭 <b>Принят автоматически</b>\n\n" +
        "👇 <b>Текст отчёта:</b>\n" +
        "<blockquote>{2}</blockquote>\n\n" +
        "📝 <i>Нажмите на кнопку, если хотите указать замечания или рекомендации для улучшения.</i>";

    /// <summary>
    /// Сообщение для администратора о новом отчёте пользователя.
    /// Содержит имя пользователя и текст отчёта, а также приглашение оставить комментарий.
    /// </summary>
    private const string ReportAnalyzerSubmissionMessage =
        "📄 <b>Новый отчёт от пользователя</b> <i>{0}</i>\n" +
        "🧑‍🏭 <b>Должность:</b> <i>{1}</i>\n\n" +
        "🧑‍🏭 <b>Проверен анализатором отчётов</b>\n\n" +
        "👇 <b>Текст отчёта:</b>\n" +
        "<blockquote>{2}</blockquote>\n\n" +
        "📝 <i>Нажмите на кнопку, если хотите указать замечания или рекомендации для улучшения.</i>";

    /// <summary>
    /// Сообщение для администратора о новом отчёте, требующем проверки.
    /// Содержит имя пользователя, должность и текст отчёта.
    /// </summary>
    private const string ReportVerificationRequestMessage =
        "📢 <b>Требуется проверка отчёта</b> от <i>{0}</i>\n" +
        "🧑‍💼 <b>Должность:</b> <i>{1}</i>\n\n" +
        "🔍 <b>Отчёт ожидает вашего решения</b>\n\n" +
        "👇 <b>Текст отчёта:</b>\n" +
        "<blockquote>{2}</blockquote>\n\n" +
        "📌 <i>Используйте кнопки ниже для подтверждения или отклонения отчёта</i>";

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет о создании нового отчёта.
    /// </summary>
    public async Task NotifyNewReportAsync(Report report, User? reviewer = null, CancellationToken token = default)
    {
        // Проверяем, что отчёт привязан к пользователю
        if (report.User == null)
            throw new ArgumentException("Please set the User navigation property in the Report");

        // Получаем список ID всех администраторов системы
        var admins = await unitOfWork
            .Query<User>()
            .Where(u => u.Role == Role.Admin || u.Role == Role.TeleAdmin)
            .Select(u => u.Id)
            .ToListAsync(token);

        // Формируем список чатов для уведомлений, исключая автора отчёта
        var chatsToNotify = admins
            .Where(a => a != report.User!.Id) // Исключаем автора отчёта из получателей
            .Select(a => new ValueTuple<long, int?>(a, null)) // Создаём кортежи (chatId, threadId)
            .ToList();

        // Если у пользователя есть рабочий чат, добавляем его в список получателей
        if (report.User.WorkingChat != null)
        {
            chatsToNotify.Add(new ValueTuple<long, int?>(
                report.User.WorkingChat.Id,
                report.User.WorkingChat.MessageThreadId));
        }

        // Определяем тип отчёта (утренний/вечерний)
        var userReport = report.EveningReport ?? report.MorningReport;

        string message;

        // Формируем сообщение в зависимости от статуса и способа подтверждения отчёта
        if (userReport.Approved)
        {
            // Если отчёт подтверждён вручную администратором
            if (reviewer != null)
            {
                message = string.Format(
                    ReportHandSubmissionMessage, // Шаблон для ручного подтверждения
                    report.User.FullName, // 0: Имя пользователя
                    report.User.Position, // 1: Должность
                    reviewer.FullName, // 2: Имя подтверждающего
                    reviewer.Position, // 3: Должность подтверждающего
                    userReport.Data); // 4: Текст отчёта
            }
            else
            {
                // Если отчёт подтверждён автоматически анализатором
                message = string.Format(
                    ReportAnalyzerSubmissionMessage, // Шаблон для автоматического подтверждения
                    report.User.FullName, // 0: Имя пользователя
                    report.User.Position, // 1: Должность
                    userReport.Data); // 2: Текст отчёта
            }
        }
        else if (userReport.ApprovedBySystem)
        {
            // Если отчёт подтверждён системой (без анализатора)
            message = string.Format(
                ReportSystemSubmissionMessage, // Шаблон для системного подтверждения
                report.User.FullName, // 0: Имя пользователя
                report.User.Position, // 1: Должность
                userReport.Data); // 2: Текст отчёта
        }
        else
        {
            // Если отчёт не подтверждён ни одним из способов
            throw new ArgumentException("Report is not approved");
        }

        // Создаём клавиатуру с кнопкой для комментариев
        var keyboard = AdminKeyboard.CommentReportKeyboard(
            report.Id,
            report.EveningReport != null); // Флаг типа отчёта

        // Отправляем уведомления всем получателям
        await SendMessagesAsync(chatsToNotify, message, keyboard, token);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет о необходимости анализа и проверки отчёта.
    /// </summary>
    public async Task NotifyVerifyReportAsync(Report report, CancellationToken token = default)
    {
        // Проверяем, что отчёт привязан к пользователю
        if (report.User == null)
            throw new ArgumentException("Please set the User navigation property in the Report");

        // Получаем список ID всех администраторов системы
        var admins = await unitOfWork
            .Query<User>()
            .Where(u => u.Role == Role.Admin || u.Role == Role.TeleAdmin)
            .Select(u => u.Id)
            .ToListAsync(token);
        
        // Формируем список чатов для уведомлений, исключая автора отчёта
        var chatsToNotify = admins
            .Where(a => a != report.User!.Id) // Исключаем автора отчёта из получателей
            .ToList();

        // Определяем тип отчёта (утренний/вечерний)
        var userReport = report.EveningReport ?? report.MorningReport;

        // Формируем сообщение с запросом проверки
        var message = string.Format(
            ReportVerificationRequestMessage, // Шаблон запроса проверки
            report.User.FullName, // 0: Имя пользователя
            report.User.Position, // 1: Должность
            userReport.Data); // 2: Текст отчёта

        // Создаём клавиатуру с кнопками подтверждения/отклонения
        var keyboard = AdminKeyboard.VerifyReportKeyboard(
            report.Id,
            report.EveningReport != null); // Флаг типа отчёта

        // Отправляем уведомления всем администраторам
        await SendMessagesAsync(chatsToNotify, message, keyboard, token);
    }

    /// <summary>
    /// Отправляет сообщения пользователям параллельно.
    /// </summary>
    /// <param name="chats">Список ID пользователей для отправки сообщений</param>
    /// <param name="message">Текст сообщения для отправки</param>
    /// <param name="replyMarkup">Опциональная клавиатура или разметка для сообщения</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции</param>
    /// <returns>Task, представляющий асинхронную операцию массовой отправки</returns>
    /// <remarks>
    /// Метод преобразует простые идентификаторы чатов в кортежи с null threadId
    /// и делегирует отправку основному методу SendMessagesAsync
    /// </remarks>
    private Task SendMessagesAsync(
        IEnumerable<long> chats,
        string message,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        // Преобразуем плоский список ID чатов в кортежи (chatId, null threadId)
        var chatsToSend = chats.Select(c => new ValueTuple<long, int?>(c, null));

        // Вызываем основной метод отправки с преобразованными данными
        return SendMessagesAsync(chatsToSend, message, replyMarkup, cancellationToken);
    }

    /// <summary>
    /// Отправляет сообщения пользователям параллельно.
    /// </summary>
    /// <param name="chats">Список ID пользователей.</param>
    /// <param name="message">Текст сообщения.</param>
    /// <param name="replyMarkup">Опциональная клавиатура или разметка для сообщения.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task SendMessagesAsync(
        IEnumerable<(long chatId, int? threadId)> chats,
        string message,
        ReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        // Устанавливаем токен отмены операции
        options.CancellationToken = cancellationToken;

        // Параллельная отправка сообщений каждому пользователю из списка.
        await Parallel.ForEachAsync(chats, options, async (chat, ct) =>
        {
            try
            {
                // Отправка сообщения пользователю через Telegram API.
                await client.SendMessage(
                    chatId: chat.chatId, // ID пользователя, которому отправляется сообщение.
                    messageThreadId: chat.threadId, // ID треда в чате
                    text: message, // Текст сообщения.
                    parseMode: ParseMode.Html, // Режим парсинга текста (HTML).
                    replyMarkup: replyMarkup, // Клавиатура сообщения.
                    cancellationToken: ct // Токен отмены для текущей операции.
                );
            }
            catch (ApiRequestException ex) // Обработка ошибок, связанных с запросами к Telegram API.
            {
                // Логирование ошибки, если не удалось отправить сообщение.
                logger.LogWarning(ex, "Failed to send message to chat {ChatId}.", chat.chatId);
            }
        });
    }
}