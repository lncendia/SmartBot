namespace SmartBot.Abstractions.Interfaces.Utils;

/// <summary>
/// Интерфейс для провайдера даты и времени.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Возвращает текущее время.
    /// </summary>
    DateTime Now { get; }
}
