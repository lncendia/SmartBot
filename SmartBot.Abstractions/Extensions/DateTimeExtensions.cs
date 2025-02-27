namespace SmartBot.Abstractions.Extensions;

/// <summary>
/// Класс расширений для работы с DateTime.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Проверяет, является ли текущее время подходящим для утреннего отчета (с 9:00 до 10:00).
    /// </summary>
    /// <param name="dateTime">Дата и время для проверки.</param>
    /// <returns>true, если время подходит для утреннего отчета; в противном случае — false.</returns>
    public static bool IsMorningReportTime(this DateTime dateTime)
    {
        // 09:00 - 10:00 и не выходной день
        return dateTime.Hour is >= 9 and < 10 && !dateTime.IsWeekend();
    }
    
    /// <summary>
    /// Проверяет, является ли текущее время подходящим для вечернего отчета (с 18:00 до 19:00).
    /// </summary>
    /// <param name="dateTime">Дата и время для проверки.</param>
    /// <returns>true, если время подходит для вечернего отчета; в противном случае — false.</returns>
    public static bool IsEveningReportTime(this DateTime dateTime)
    {
        // 18:00 - 19:00
        return dateTime.Hour is >= 18 and < 19 && !dateTime.IsWeekend();
    }

    /// <summary>
    /// Проверяет, является ли указанная дата выходным днем (суббота или воскресенье).
    /// </summary>
    /// <param name="dateTime">Дата для проверки.</param>
    /// <returns>true, если дата является выходным днем; в противном случае — false.</returns>
    public static bool IsWeekend(this DateTime dateTime)
    {
        // Суббота или воскресенье
        return dateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }
}