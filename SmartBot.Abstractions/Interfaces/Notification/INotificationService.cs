using SmartBot.Abstractions.Models.Reports;
using SmartBot.Abstractions.Models.Users;

namespace SmartBot.Abstractions.Interfaces.Notification;

/// <summary>
/// Интерфейс для уведомлений о событиях, связанных с отчётами.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Уведомляет о необходимости сдать утренний отчёт.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывается, когда наступает время сдачи утреннего отчёта.
    /// </remarks>
    Task NotifyMorningReportDueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Уведомляет о необходимости сдать вечерний отчёт.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывается, когда наступает время сдачи вечернего отчёта.
    /// </remarks>
    Task NotifyEveningReportDueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Уведомляет о том, что время сдачи утреннего отчёта подходит к концу.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывается, когда остаётся мало времени до окончания срока сдачи утреннего отчёта.
    /// </remarks>
    Task NotifyMorningReportDeadlineApproachingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Уведомляет о том, что время сдачи вечернего отчёта подходит к концу.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывается, когда остаётся мало времени до окончания срока сдачи вечернего отчёта.
    /// </remarks>
    Task NotifyEveningReportDeadlineApproachingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Уведомляет о том, что утренний отчёт не был сдан.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывается, если утренний отчёт не был сдан в установленный срок.
    /// </remarks>
    Task NotifyMorningReportMissedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Уведомляет пользователя о том, что вечерний отчёт не был сдан в установленный срок.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции</param>
    /// <remarks>
    /// Отправляет пользователю напоминание о необходимости сдать пропущенный вечерний отчёт.
    /// Включает информацию о последствиях пропуска отчёта.
    /// </remarks>
    Task NotifyEveningReportMissedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Уведомляет о создании нового отчёта.
    /// </summary>
    /// <param name="report">Объект отчёта, содержащий данные для проверки</param>
    /// <param name="reviewer">Администратор, который проверил отчёт. 
    /// Если null - отчёт проверен автоматически.</param>
    /// <param name="token">Токен отмены для асинхронной операции</param>
    /// <remarks>
    /// Используется для оповещения администраторов о новых отчётах.
    /// </remarks>
    Task NotifyNewReportAsync(Report report, User? reviewer = null, CancellationToken token = default);

    /// <summary>
    /// Уведомляет о необходимости анализа и проверки отчёта.
    /// </summary>
    /// <param name="report">Объект отчёта, требующий проверки</param>
    /// <param name="token">Токен отмены для асинхронной операции</param>
    /// <remarks>
    /// Отправляется администраторам для ручной проверки отчётов,
    /// которые не могут быть автоматически обработаны системой.
    /// </remarks>
    Task NotifyVerifyReportAsync(Report report, CancellationToken token = default);
}