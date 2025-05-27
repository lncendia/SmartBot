using SmartBot.Abstractions.Models.Reports;
using Telegram.Bot.Types;

namespace SmartBot.Abstractions.Interfaces.Utils;

/// <summary>
/// Сервис для отправки мотивационных сообщений пользователям на основе их отчетов
/// </summary>
public interface IMotivationalMessageService
{
    /// <summary>
    /// Отправляет мотивационные сообщения пользователю в зависимости от типа отчёта.
    /// Определяет тип отчёта (утренний/вечерний) и вызывает соответствующие обработчики.
    /// </summary>
    /// <param name="chatId">Идентификатор чата с пользователем</param>
    /// <param name="replyMessageId">Идентификатор сообщения для ответа</param>
    /// <param name="report">Объект отчёта, содержащий данные утреннего или вечернего отчета</param>
    /// <param name="ct">Токен отмены для асинхронных операций</param>
    /// <returns>Task, представляющий асинхронную операцию отправки сообщений</returns>
    Task SendMotivationalMessagesAsync(
        ChatId chatId,
        int replyMessageId,
        Report report,
        CancellationToken ct);
}