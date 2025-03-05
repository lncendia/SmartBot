namespace SmartBot.Abstractions.Extensions;

/// <summary>
/// Класс расширений для работы с DateTime.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Проверяет, является ли текущее время рабочим (с 9:00 до 22:00) и не выходным днём.
    /// </summary>
    /// <param name="dateTime">Дата и время для проверки.</param>
    /// <returns>true, если время является рабочим; в противном случае — false.</returns>
    public static bool IsWorkingPeriod(this DateTime dateTime)
    {
        // Проверяем, что время находится в диапазоне с 9:00 до 22:00 и это не выходной день.
        return dateTime.Hour is >= 9 and < 22 && !dateTime.IsWeekend();
    }

    /// <summary>
    /// Проверяет, является ли текущее время подходящим для вечернего отчёта (с 18:00).
    /// </summary>
    /// <param name="dateTime">Дата и время для проверки.</param>
    /// <returns>true, если время подходит для вечернего отчёта; в противном случае — false.</returns>
    public static bool IsEveningPeriod(this DateTime dateTime)
    {
        // Проверяем, что время больше или равно 18:00.
        return dateTime.Hour >= 18;
    }

    /// <summary>
    /// Проверяет, просрочен ли утренний отчёт (дедлайн — 10:00).
    /// Если текущее время позже 10:00, возвращает время, на которое отчёт был просрочен.
    /// </summary>
    /// <param name="dateTime">Дата и время для проверки.</param>
    /// <returns>
    /// null, если время до 10:00 (отчёт не просрочен);
    /// иначе — время, на которое отчёт был просрочен.
    /// </returns>
    public static TimeSpan? MorningReportOverdue(this DateTime dateTime)
    {
        // Если время до 10:00, отчёт не просрочен.
        if (dateTime.Hour < 10) return null;

        // Возвращаем разницу между текущим временем и 10:00.
        return dateTime - dateTime.Date.AddHours(10);
    }

    /// <summary>
    /// Проверяет, просрочен ли вечерний отчёт (дедлайн — 19:00).
    /// Если текущее время позже 19:00, возвращает время, на которое отчёт был просрочен.
    /// </summary>
    /// <param name="dateTime">Дата и время для проверки.</param>
    /// <returns>
    /// null, если время до 19:00 (отчёт не просрочен);
    /// иначе — время, на которое отчёт был просрочен.
    /// </returns>
    public static TimeSpan? EveningReportOverdue(this DateTime dateTime)
    {
        // Если время до 19:00, отчёт не просрочен.
        if (dateTime.Hour < 19) return null;

        // Возвращаем разницу между текущим временем и 19:00.
        return dateTime - dateTime.Date.AddHours(19);
    }

    /// <summary>
    /// Проверяет, является ли указанная дата выходным днем (суббота или воскресенье).
    /// </summary>
    /// <param name="dateTime">Дата для проверки.</param>
    /// <returns>true, если дата является выходным днем; в противном случае — false.</returns>
    public static bool IsWeekend(this DateTime dateTime)
    {
        return false; //todo: for testing

        // Суббота или воскресенье
        return dateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }
}