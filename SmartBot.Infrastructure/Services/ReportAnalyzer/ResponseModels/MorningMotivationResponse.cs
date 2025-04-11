using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.ResponseModels;

/// <summary>
/// Класс, представляющий утреннюю мотивационную сводку
/// </summary>
/// <remarks>
/// Используется для генерации мотивационного контента
/// на основе планов на день
/// </remarks>
public class MorningMotivationResponse
{
    /// <summary>
    /// Рекомендации по планированию рабочего дня
    /// </summary>
    /// <value>
    /// Список советов по организации работы,
    /// обычно содержит 2-3 конкретных рекомендации
    /// </value>
    [JsonPropertyName("recommendations")]
    public required string Recommendations { get; init; }
    
    /// <summary>
    /// Мотивирующее сообщение
    /// </summary>
    /// <value>
    /// Вдохновляющий текст для начала рабочего дня,
    /// обычно 2-3 предложения позитивного содержания
    /// </value>
    [JsonPropertyName("motivation")]
    public required string Motivation { get; init; }
    
    /// <summary>
    /// Юмористическая ремарка
    /// </summary>
    /// <value>
    /// Легкая шутка или забавный факт,
    /// обычно 1-2 предложения для поднятия настроения
    /// </value>
    [JsonPropertyName("humor")]
    public required string Humor { get; init; }
}