namespace SmartBot.Abstractions.Models;

/// <summary>
/// Класс, представляющий отчёт пользователя (утренний или вечерний).
/// </summary>
public class UserReport
{
    /// <summary>
    /// Данные отчёта (например, текст или структурированные данные).
    /// </summary>
    public required string Data { get; init; }

    /// <summary>
    /// Время, на которое отчёт был просрочен (если отчёт сдан с опозданием).
    /// Если отчёт сдан вовремя, значение равно null.
    /// </summary>
    public TimeSpan? Overdue { get; init; }
}