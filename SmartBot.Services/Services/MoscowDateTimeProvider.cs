using SmartBot.Abstractions.Interfaces.Utils;

namespace SmartBot.Services.Services;

/// <summary>
/// Провайдер даты и времени для часового пояса UTC+3 (МСК).
/// </summary>
public class MoscowDateTimeProvider : IDateTimeProvider
{
    /// <summary>
    /// Часовой пояс Москвы.
    /// </summary>
    private static readonly TimeZoneInfo MoscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

    /// <summary>
    /// Возвращает текущее время в часовом поясе МСК (UTC+3).
    /// </summary>
    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MoscowTimeZone);
}
