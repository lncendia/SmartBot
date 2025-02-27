using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.Services;

/// <summary>
/// Сервис для отправки уведомлений пользователям через Telegram.
/// </summary>
/// <param name="client">Клиент Telegram Bot API для отправки сообщений.</param>
/// <param name="unitOfWork">Единица работы для взаимодействия с базой данных.</param>
/// <param name="dateTimeProvider">Провайдер времени для работы с датами и временем.</param>
/// <param name="logger">Логгер для записи событий и ошибок.</param>
public class NotificationService(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<NotificationService> logger) : INotificationService
{
    /// <summary>
    /// Шаблон сообщения для уведомления о необходимости сдать утренний отчёт.
    /// </summary>
    private const string MorningReportMessage = "<b>Доброе утро! 🌞</b>\n\n" +
                                                "Не забудьте отправить ваш <b>утренний отчёт</b>. " +
                                                "Это поможет вам начать день с чёткого плана и продуктивности!";

    /// <summary>
    /// Шаблон сообщения для уведомления о необходимости сдать вечерний отчёт.
    /// </summary>
    private const string EveningReportMessage = "<b>Добрый вечер! 🌇</b>\n\n" +
                                                "Пожалуйста, найдите время, чтобы отправить ваш <b>вечерний отчёт</b>. " +
                                                "Это важно для подведения итогов дня и планирования завтрашних задач.";

    /// <summary>
    /// Шаблон сообщения для уведомления о том, что время сдачи утреннего отчёта подходит к концу.
    /// </summary>
    private const string MorningDeadlineApproachingMessage =
        "<b>Напоминание: Время сдачи утреннего отчёта истекает ⏰</b>\n\n" +
        "У вас осталось меньше часа, чтобы отправить ваш <b>утренний отчёт</b>. " +
        "Не упустите возможность начать день с ясными целями!";

    /// <summary>
    /// Шаблон сообщения для уведомления о том, что время сдачи вечернего отчёта подходит к концу.
    /// </summary>
    private const string EveningDeadlineApproachingMessage =
        "<b>Напоминание: Время сдачи вечернего отчёта истекает ⏰</b>\n\n" +
        "У вас осталось меньше часа, чтобы отправить ваш <b>вечерний отчёт</b>. " +
        "Не забудьте подвести итоги дня!";

    /// <summary>
    /// Шаблон сообщения для уведомления о том, что утренний отчёт не был сдан.
    /// </summary>
    private const string MorningReportMissedMessage = "<b>Внимание: Утренний отчёт не сдан ⚠️</b>\n\n" +
                                                      "Вы не отправили ваш <b>утренний отчёт</b> вовремя. " +
                                                      "Пожалуйста, постарайтесь не пропускать отчёты, чтобы оставаться в курсе ваших задач и целей.";

    /// <summary>
    /// Шаблон сообщения для уведомления о том, что вечерний отчёт не был сдан.
    /// </summary>
    private const string EveningReportMissedMessage = "<b>Внимание: Вечерний отчёт не сдан ⚠️</b>\n\n" +
                                                      "Вы не отправили ваш <b>вечерний отчёт</b> вовремя. " +
                                                      "Пожалуйста, постарайтесь не пропускать отчёты, чтобы эффективно планировать свои задачи.";

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

            // Выбираем только ID пользователей.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);


        // Настраиваем параметры параллельного выполнения.
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = cancellationToken };

        // Параллельно отправляем уведомления каждому пользователю.
        await Parallel.ForEachAsync(users, parallelOptions, async (userId, ct) =>
        {
            try
            {
                // Отправляем сообщение пользователю.
                await client.SendMessage(
                    chatId: userId,
                    text: MorningReportMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct
                );
            }
            catch (ApiRequestException ex) // Ловим только ошибки Telegram API
            {
                // Логируем ошибку, если не удалось отправить сообщение.
                logger.LogWarning(ex, "Failed to send morning report notification to user {UserId}.", userId);
            }
        });
    }

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет пользователей о необходимости сдать вечерний отчёт.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task NotifyEveningReportDueAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время.
        var now = dateTimeProvider.Now;

        // Находим пользователей, которые ещё не сдали вечерний отчёт за текущий день.
        var users = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых нет вечернего отчёта за текущий день.
            .Where(u => u.Reports.FirstOrDefault(r => r.Date.Date == now.Date)!.EveningReport ==
                        null) // todo: проверить

            // Выбираем только ID пользователей.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Настраиваем параметры параллельного выполнения.
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = cancellationToken };

        // Параллельно отправляем уведомления каждому пользователю.
        await Parallel.ForEachAsync(users, parallelOptions, async (userId, ct) =>
        {
            try
            {
                // Отправляем сообщение пользователю.
                await client.SendMessage(
                    chatId: userId,
                    text: EveningReportMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct
                );
            }
            catch (ApiRequestException ex) // Ловим только ошибки Telegram API
            {
                // Логируем ошибку, если не удалось отправить сообщение.
                logger.LogWarning(ex, "Failed to send evening report notification to user {UserId}.", userId);
            }
        });
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

            // Выбираем только ID пользователей.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Настраиваем параметры параллельного выполнения.
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = cancellationToken };

        // Параллельно отправляем уведомления каждому пользователю.
        await Parallel.ForEachAsync(users, parallelOptions, async (userId, ct) =>
        {
            try
            {
                // Отправляем сообщение пользователю.
                await client.SendMessage(
                    chatId: userId,
                    text: MorningDeadlineApproachingMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct
                );
            }
            catch (ApiRequestException ex) // Ловим только ошибки Telegram API
            {
                // Логируем ошибку, если не удалось отправить сообщение.
                logger.LogWarning(ex, "Failed to send morning report deadline notification to user {UserId}.", userId);
            }
        });
    }

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет пользователей о том, что время сдачи вечернего отчёта подходит к концу.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task NotifyEveningReportDeadlineApproachingAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время.
        var now = dateTimeProvider.Now;

        // Находим пользователей, которые ещё не сдали вечерний отчёт за текущий день.
        var users = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых нет вечернего отчёта за текущий день.
            .Where(u => u.Reports.FirstOrDefault(r => r.Date.Date == now.Date)!.EveningReport ==
                        null) // todo: проверить

            // Выбираем только ID пользователей.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Настраиваем параметры параллельного выполнения.
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = cancellationToken };

        // Параллельно отправляем уведомления каждому пользователю.
        await Parallel.ForEachAsync(users, parallelOptions, async (userId, ct) =>
        {
            try
            {
                // Отправляем сообщение пользователю.
                await client.SendMessage(
                    chatId: userId,
                    text: EveningDeadlineApproachingMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct
                );
            }
            catch (ApiRequestException ex) // Ловим только ошибки Telegram API
            {
                // Логируем ошибку, если не удалось отправить сообщение.
                logger.LogWarning(ex, "Failed to send evening report deadline notification to user {UserId}.", userId);
            }
        });
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

            // Выбираем только ID пользователей.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Настраиваем параметры параллельного выполнения.
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = cancellationToken };

        // Параллельно отправляем уведомления каждому пользователю.
        await Parallel.ForEachAsync(users, parallelOptions, async (userId, ct) =>
        {
            try
            {
                // Отправляем сообщение пользователю.
                await client.SendMessage(
                    chatId: userId,
                    text: MorningReportMissedMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct
                );
            }
            catch (ApiRequestException ex) // Ловим только ошибки Telegram API
            {
                // Логируем ошибку, если не удалось отправить сообщение.
                logger.LogWarning(ex, "Failed to send morning report missed notification to user {UserId}.", userId);
            }
        });
    }

    /// <inheritdoc/>
    /// <summary>
    /// Уведомляет пользователей о том, что вечерний отчёт не был сдан.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task NotifyEveningReportMissedAsync(CancellationToken cancellationToken = default)
    {
        // Получаем текущее время.
        var now = dateTimeProvider.Now;

        // Находим пользователей, которые ещё не сдали вечерний отчёт за текущий день.
        var users = await unitOfWork.Query<User>()

            // Фильтруем пользователей, у которых нет вечернего отчёта за текущий день.
            .Where(u => u.Reports.FirstOrDefault(r => r.Date.Date == now.Date)!.EveningReport ==
                        null) // todo: проверить

            // Выбираем только ID пользователей.
            .Select(u => u.Id)

            // Преобразуем результат в список.
            .ToListAsync(cancellationToken);

        // Настраиваем параметры параллельного выполнения.
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = cancellationToken };

        // Параллельно отправляем уведомления каждому пользователю.
        await Parallel.ForEachAsync(users, parallelOptions, async (userId, ct) =>
        {
            try
            {
                // Отправляем сообщение пользователю.
                await client.SendMessage(
                    chatId: userId,
                    text: EveningReportMissedMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct
                );
            }
            catch (ApiRequestException ex) // Ловим только ошибки Telegram API
            {
                // Логируем ошибку, если не удалось отправить сообщение.
                logger.LogWarning(ex, "Failed to send evening report missed notification to user {UserId}.", userId);
            }
        });
    }
}