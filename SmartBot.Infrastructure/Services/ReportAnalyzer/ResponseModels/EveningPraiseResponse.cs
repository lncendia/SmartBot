using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.ResponseModels;

/// <summary>
/// Класс, представляющий вечернюю оценочную сводку
/// </summary>
/// <remarks>
/// Содержит оценку выполненной работы за день
/// </remarks>
public class EveningPraiseResponse
{
    /// <summary>
    /// Признание достижений
    /// </summary>
    /// <value>
    /// Перечень выполненных задач и достижений,
    /// обычно 2-3 предложения с акцентом на успехи
    /// </value>
    [JsonPropertyName("achievements")]
    public required string Achievements { get; init; }
    
    /// <summary>
    /// Похвала за проделанную работу
    /// </summary>
    /// <value>
    /// Положительная оценка продуктивности,
    /// обычно 2-3 предложения с выражением признательности
    /// </value>
    [JsonPropertyName("praise")]
    public required string Praise { get; init; }
    
    /// <summary>
    /// Юмористическая ремарка
    /// </summary>
    /// <value>
    /// Шутка или забавное наблюдение по итогам дня,
    /// обычно 1-2 предложения для снятия напряжения
    /// </value>
    [JsonPropertyName("humor")]
    public required string Humor { get; init; }
}