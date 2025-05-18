using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Configuration;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces.ReportAnalyzer;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Interfaces.Utils;
using SmartBot.Abstractions.Models.Reports;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = SmartBot.Abstractions.Models.Users.User;

namespace SmartBot.Services.Services;

/// <summary>
/// Сервис для отправки мотивационных сообщений пользователям на основе их отчетов
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="reportAnalyzer">Анализатор отчётов.</param>
/// <param name="analyzerConfiguration">Конфигурация анализатора отчётов.</param>
public class MotivationalMessageService(
    ITelegramBotClient client,
    IReportAnalyzer reportAnalyzer,
    ReportAnalysisConfiguration analyzerConfiguration,
    IUnitOfWork unitOfWork
) : IMotivationalMessageService
{
    /// <summary>
    /// Шаблон сообщения о прогрессе пользователя
    /// </summary>
    private const string RankProgressMessage =
        "<b>🏆 Ваш прогресс:</b>\n\n" +
        "{0}\n" +
        "📊 <b>Текущий рейтинг:</b> {1:N2} очков\n" +
        "🎯 <b>До следующего звания:</b> {2:N2} очков\n" +
        "👥 <b>Пользователей позади вас:</b> {3}";

    /// <inheritdoc/>
    /// <summary>
    /// Отправляет мотивационные сообщения пользователю в зависимости от типа отчёта.
    /// Определяет тип отчёта (утренний/вечерний) и вызывает соответствующие обработчики.
    /// </summary>
    public async Task SendMotivationalMessagesAsync(
        ChatId chatId,
        int replyMessageId,
        Report report,
        User user,
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
                    chatId, replyMessageId,
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
                    chatId, replyMessageId,
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
                    chatId,
                    user,
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
    /// <param name="chatId">Идентификатор чата с пользователем</param>
    /// <param name="replyMessageId">Идентификатор сообщения для ответа</param>
    /// <param name="reportData">Текст утреннего отчета для анализа</param>
    /// <param name="ct">Токен отмены для асинхронных операций</param>
    /// <returns>Task, представляющий асинхронную операцию отправки сообщений</returns>
    private async Task SendMorningMotivationAsync(ChatId chatId, int replyMessageId, string reportData,
        CancellationToken ct)
    {
        // Генерируем мотивацию на основе утреннего отчета
        var motivation = reportAnalyzer.GenerateMorningMotivationAsync(reportData, ct);
        await SendTypingWhileWaitingAsync(chatId, motivation, ct);

        // Отправляем три типа сообщений последовательно:
        // 1. Основная мотивация
        await client.SendMessage(
            replyParameters: new ReplyParameters { MessageId = replyMessageId },
            chatId: chatId,
            text: motivation.Result.Motivation,
            cancellationToken: ct
        );

        // Отправляем индикатор "печатает" в чат пользователя.
        await client.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);

        // Создаем задачу задержки на 1 секунду с учетом токена отмены
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        // 2. Рекомендации на день
        await client.SendMessage(
            replyParameters: new ReplyParameters { MessageId = replyMessageId },
            chatId: chatId,
            text: motivation.Result.Recommendations,
            cancellationToken: ct
        );

        // Отправляем индикатор "печатает" в чат пользователя.
        await client.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);

        // Создаем задачу задержки на 1 секунду с учетом токена отмены
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        // 3. Юмористическое завершение
        await client.SendMessage(
            chatId: chatId,
            text: motivation.Result.Humor,
            cancellationToken: ct
        );
    }

    /// <summary>
    /// Отправляет вечерние похвалу и оценку пользователю
    /// </summary>
    /// <param name="chatId">Идентификатор чата с пользователем</param>
    /// <param name="replyMessageId">Идентификатор сообщения для ответа</param>
    /// <param name="reportData">Текст утреннего отчета</param>
    /// <param name="ct">Токен отмены</param>
    private async Task SendEveningPraiseAsync(ChatId chatId, int replyMessageId, string reportData,
        CancellationToken ct)
    {
        // Генерируем похвалу на основе вечернего отчета
        var praise = reportAnalyzer.GenerateEveningPraiseAsync(reportData, CancellationToken.None);
        await SendTypingWhileWaitingAsync(chatId, praise, ct);

        // Отправляем три типа сообщений:
        // 1. Достижения за день
        await client.SendMessage(
            replyParameters: new ReplyParameters { MessageId = replyMessageId },
            chatId: chatId,
            text: praise.Result.Achievements,
            cancellationToken: ct
        );

        // Отправляем индикатор "печатает" в чат пользователя.
        await client.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);

        // Создаем задачу задержки на 1 секунду с учетом токена отмены
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        // 2. Похвалу за проделанную работу
        await client.SendMessage(
            replyParameters: new ReplyParameters { MessageId = replyMessageId },
            chatId: chatId,
            text: praise.Result.Praise,
            cancellationToken: ct
        );

        // Отправляем индикатор "печатает" в чат пользователя.
        await client.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);

        // Создаем задачу задержки на 1 секунду с учетом токена отмены
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        // 3. Юмористическое завершение дня
        await client.SendMessage(
            chatId: chatId,
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
    /// <param name="chatId">Идентификатор чата с пользователем</param>
    /// <param name="user">Автор отчёта</param>
    /// <param name="reportData">Текст вечернего отчета для анализа</param>
    /// <param name="ct">Токен отмены для асинхронных операций</param>
    private async Task ProcessUserScoreAsync(ChatId chatId, User user, string reportData, CancellationToken ct)
    {
        // Запускаем асинхронную задачу для получения оценки отчета
        var scoreTask = reportAnalyzer.GetScorePointsAsync(reportData, ct);

        // Пока задача выполняется, периодически отправляем индикатор "печатает"
        await SendTypingWhileWaitingAsync(chatId, scoreTask, ct);

        // Получаем результат выполнения задачи - количество заработанных очков
        var earnedScore = scoreTask.Result;

        // Сохраняем текущее звание ДО начисления очков для последующего сравнения
        var previousRank = user.Rank;

        // Начисляем очки, если они положительные
        if (earnedScore > 0) user.Score += earnedScore;

        // Сохраняем изменения
        await unitOfWork.SaveChangesAsync(ct);

        // Получаем актуальное звание ПОСЛЕ начисления очков
        var currentRank = user.Rank;

        // Вычисляем сколько очков осталось до следующего звания
        var pointsRemaining = user.PointsToNextRank;

        // Запрос в БД для подсчета количества пользователей с меньшим рейтингом
        var usersBehindCount = await unitOfWork.Query<User>()
            .Where(u => u.Role == Role.Employee || u.Role == Role.TeleAdmin)
            .Where(u => u.Score < user.Score)
            .CountAsync(ct);

        // Формируем основное сообщение в зависимости от изменения звания
        var statusMessage = previousRank == currentRank
            ? $"📈 <b>Вы</b> ({currentRank}) стали ближе к новому званию!"
            : $"🎉 Поздравляем с повышением до <b>{currentRank}!</b>";

        // Формируем итоговое сообщение, подставляя данные в шаблон
        var message = string.Format(RankProgressMessage,
            statusMessage,
            user.Score,
            pointsRemaining,
            usersBehindCount);

        // Отправляем сформированное сообщение пользователю
        await client.SendMessage(
            chatId: chatId,
            text: message,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    /// <summary>
    /// Отправляет статус "печатает" пока задача не завершена.
    /// </summary>
    /// <param name="chatId">Идентификатор чата с пользователем</param>
    /// <param name="task">Задача, за которой нужно следить.</param>
    /// <param name="ct">Токен отмены операции.</param>
    private async Task SendTypingWhileWaitingAsync(ChatId chatId, Task task, CancellationToken ct)
    {
        while (!task.IsCompleted)
        {
            await client.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);
            await Task.Delay(5000, ct);
        }
    }
}