﻿namespace SmartBot.Abstractions.Interfaces;

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
    /// Уведомляет о том, что вечерний отчёт не был сдан.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывается, если вечерний отчёт не был сдан в установленный срок.
    /// </remarks>
    Task NotifyEveningReportMissedAsync(CancellationToken cancellationToken = default);
}