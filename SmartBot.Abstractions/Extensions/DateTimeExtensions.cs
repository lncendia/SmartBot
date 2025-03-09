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
        return dateTime.Hour >= 9 && !dateTime.IsWeekend();
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
        // Суббота или воскресенье
        return dateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }
    
    /// <summary>
    /// Проверяет, является ли предыдущий день (относительно указанной даты) выходным.
    /// Используется для фоновых процессов, которые запускаются в 00:00 следующего дня.
    /// Например:
    /// - Для понедельника проверяется воскресенье.
    /// - Для субботы проверяется пятница.
    /// </summary>
    /// <param name="dateTime">Дата, для которой проверяется предыдущий день.</param>
    /// <returns>true, если предыдущий день является выходным; в противном случае — false.</returns>
    public static bool IsPreviousDayWeekend(this DateTime dateTime)
    {
        // Получаем предыдущий день
        var previousDay = dateTime.AddDays(-1);

        // Проверяем, является ли предыдущий день выходным
        return previousDay.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }
    
    /// <summary>
    /// Форматирует TimeSpan в строку в формате "Xч Yм Zс", исключая нулевые компоненты.
    /// </summary>
    /// <param name="timeSpan">Временной интервал для форматирования.</param>
    /// <returns>Отформатированная строка.</returns>
    public static string FormatTimeSpan(this TimeSpan? timeSpan)
    {
        // Если интервал не задан - возвращаем пустую строку
        if (!timeSpan.HasValue) return string.Empty;

        // Список частей строки
        var parts = new List<string>();

        // Добавляем часы, если они есть
        if (timeSpan.Value.Hours > 0) parts.Add($"{timeSpan.Value.Hours}ч");

        // Добавляем минуты, если они есть
        if (timeSpan.Value.Minutes > 0) parts.Add($"{timeSpan.Value.Minutes}м");

        // Добавляем секунды, если они есть
        if (timeSpan.Value.Seconds > 0) parts.Add($"{timeSpan.Value.Seconds}с");

        // Если все компоненты нулевые, возвращаем "0с"
        if (parts.Count == 0) return "0с";

        // Соединяем части в одну строку с пробелами
        return string.Join(" ", parts);
    }
}