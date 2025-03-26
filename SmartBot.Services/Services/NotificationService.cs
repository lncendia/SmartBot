using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

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
        "У вас осталось меньше получаса, чтобы отправить ваш <b>утренний отчёт</b>. " +
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
        "У вас осталось меньше получаса, чтобы отправить ваш <b>вечерний отчёт</b>. " +
        "Не забудьте подвести итоги дня!";

    /// <summary>
    /// Шаблон сообщения для уведомления о том, что время сдачи вечернего отчёта подходит к концу (если утренний отчёт не сдан).
    /// </summary>
    private const string EveningDeadlineApproachingMessage_MorningMissed =
        "<b>Напоминание: Время сдачи вечернего отчёта истекает ⏰</b>\n\n" +
        "У вас осталось меньше получаса, чтобы отправить ваш <b>вечерний отчёт</b>. " +
        "Однако вы ещё не отправили <b>утренний отчёт</b>. " +
        "Пожалуйста, сначала отправьте пропущенный утренний отчёт, а затем переходите к вечернему.";

    /// <summary>
    /// Шаблон сообщения для уведомления о том, что вечерний отчёт не был сдан (если утренний отчёт сдан).
    /// </summary>
    private const string EveningReportMissedMessage_MorningDone = "<b>Внимание: Вечерний отчёт не сдан ⚠️</b>\n\n" +
                                                                  "Вы не отправили ваш <b>вечерний отчёт</b> вовремя. " +
                                                                  "Вы можете отправить его сейчас, но он будет отмечен как просроченный.";

    /// <summary>
    /// Шаблон сообщения для уведомления о том, что вечерний отчёт не был сдан (если утренний отчёт не сдан).
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
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task NotifyMorningReportDueAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время.
        var now = dateTimeProvider.Now;

        // Находим пользователей, которые ещё не сдали утренний отчёт за текущий день.
        var users = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых нет отчётов за текущий день.
            .Where(u => u.Reports.All(r => r.Date.Date != now.Date))

            // Фильтруем пользователей, не являющихся администратороми
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Выбираем только ID пользователей.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Отправляем сообщения пользователям, которые не сдали утренний отчёт
        await SendMessagesAsync(users, MorningReportMessage, cancellationToken);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет пользователей о том, что время сдачи утреннего отчёта подходит к концу.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task NotifyMorningReportDeadlineApproachingAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время.
        var now = dateTimeProvider.Now;

        // Находим пользователей, которые ещё не сдали утренний отчёт за текущий день.
        var users = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых нет отчётов за текущий день.
            .Where(u => u.Reports.All(r => r.Date.Date != now.Date))

            // Фильтруем пользователей, не являющихся администратороми
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Выбираем только ID пользователей.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Отправляем сообщения пользователям, которые не сдали утренний отчёт
        await SendMessagesAsync(users, MorningDeadlineApproachingMessage, cancellationToken);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет пользователей о том, что утренний отчёт не был сдан.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task NotifyMorningReportMissedAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время.
        var now = dateTimeProvider.Now;

        // Находим пользователей, которые ещё не сдали утренний отчёт за текущий день.
        var users = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых нет отчётов за текущий день.
            .Where(u => u.Reports.All(r => r.Date.Date != now.Date))

            // Фильтруем пользователей, не являющихся администратороми
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Выбираем только ID пользователей.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Отправляем сообщения пользователям, которые не сдали утренний отчёт
        await SendMessagesAsync(users, MorningReportMissedMessage, cancellationToken);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет пользователей о необходимости сдать вечерний отчёт.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task NotifyEveningReportDueAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время для проверки отчётов за сегодняшний день.
        var now = dateTimeProvider.Now;

        // Пользователи, которые сдали утренний отчёт, но не сдали вечерний.
        var usersWithMorningReport = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых есть утренний отчёт за сегодня, но нет вечернего.
            .Where(u => u.Reports.Any(r => r.Date.Date == now.Date && r.EveningReport == null))

            // Фильтруем пользователей, не являющихся администратороми
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Выбираем только ID пользователей для дальнейшей обработки.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Пользователи, которые не сдали ни утренний, ни вечерний отчёт.
        var usersWithoutMorningReport = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых либо вообще нет отчётов за сегодня
            .Where(u => u.Reports.All(r => r.Date.Date != now.Date))

            // Фильтруем пользователей, не являющихся администратороми
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Выбираем только ID пользователей для дальнейшей обработки.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Отправляем сообщения пользователям, которые сдали утренний отчёт.
        await SendMessagesAsync(usersWithMorningReport, EveningReportMessage_MorningDone, cancellationToken);

        // Отправляем сообщения пользователям, которые не сдали утренний отчёт.
        await SendMessagesAsync(usersWithoutMorningReport, EveningReportMessage_MorningMissed, cancellationToken);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет пользователей о том, что время сдачи вечернего отчёта подходит к концу.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task NotifyEveningReportDeadlineApproachingAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время для проверки отчётов за сегодняшний день.
        var now = dateTimeProvider.Now;

        // Пользователи, которые сдали утренний отчёт, но не сдали вечерний.
        var usersWithMorningReport = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых есть утренний отчёт за сегодня, но нет вечернего.
            .Where(u => u.Reports.Any(r => r.Date.Date == now.Date && r.EveningReport == null))

            // Фильтруем пользователей, не являющихся администратороми
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Выбираем только ID пользователей для дальнейшей обработки.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Пользователи, которые не сдали ни утренний, ни вечерний отчёт.
        var usersWithoutMorningReport = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых либо вообще нет отчётов за сегодня
            .Where(u => u.Reports.All(r => r.Date.Date != now.Date))

            // Фильтруем пользователей, не являющихся администратороми
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Выбираем только ID пользователей для дальнейшей обработки.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Отправляем сообщения пользователям, которые сдали утренний отчёт
        await SendMessagesAsync(usersWithMorningReport, EveningDeadlineApproachingMessage_MorningDone,
            cancellationToken);

        // Отправляем сообщения пользователям, которые не сдали утренний отчёт
        await SendMessagesAsync(usersWithoutMorningReport, EveningDeadlineApproachingMessage_MorningMissed,
            cancellationToken);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет пользователей о том, что вечерний отчёт не был сдан.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task NotifyEveningReportMissedAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время для проверки отчётов за сегодняшний день.
        var now = dateTimeProvider.Now;

        // Пользователи, которые сдали утренний отчёт, но не сдали вечерний.
        var usersWithMorningReport = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых есть утренний отчёт за сегодня, но нет вечернего.
            .Where(u => u.Reports.Any(r => r.Date.Date == now.Date && r.EveningReport == null))

            // Фильтруем пользователей, не являющихся администратороми
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Выбираем только ID пользователей для дальнейшей обработки.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Пользователи, которые не сдали ни утренний, ни вечерний отчёт.
        var usersWithoutMorningReport = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых либо вообще нет отчётов за сегодня
            .Where(u => u.Reports.All(r => r.Date.Date != now.Date))

            // Фильтруем пользователей, не являющихся администратороми
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)

            // Выбираем только ID пользователей для дальнейшей обработки.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Отправляем сообщения пользователям, которые сдали утренний отчёт
        await SendMessagesAsync(usersWithMorningReport, EveningReportMissedMessage_MorningDone, cancellationToken);

        // Отправляем сообщения пользователям, которые не сдали утренний отчёт
        await SendMessagesAsync(usersWithoutMorningReport, EveningReportMissedMessage_MorningMissed, cancellationToken);
    }

    /// <summary>
    /// Отправляет сообщения пользователям параллельно.
    /// </summary>
    /// <param name="userIds">Список ID пользователей.</param>
    /// <param name="message">Текст сообщения.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task SendMessagesAsync(IEnumerable<long> userIds, string message, CancellationToken cancellationToken)
    {
        // Устанавливаем токен отмены операции
        options.CancellationToken = cancellationToken;

        // Параллельная отправка сообщений каждому пользователю из списка.
        await Parallel.ForEachAsync(userIds, options, async (userId, ct) =>
        {
            try
            {
                // Отправка сообщения пользователю через Telegram API.
                await client.SendMessage(
                    chatId: userId, // ID пользователя, которому отправляется сообщение.
                    text: message, // Текст сообщения.
                    parseMode: ParseMode.Html, // Режим парсинга текста (HTML).
                    cancellationToken: ct // Токен отмены для текущей операции.
                );
            }
            catch (ApiRequestException ex) // Обработка ошибок, связанных с запросами к Telegram API.
            {
                // Логирование ошибки, если не удалось отправить сообщение.
                logger.LogWarning(ex, "Failed to send message to user {UserId}.", userId);
            }
        });
    }
}