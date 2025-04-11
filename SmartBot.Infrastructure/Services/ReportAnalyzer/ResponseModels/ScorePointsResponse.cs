using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.ResponseModels;

/// <summary>
/// Класс, представляющий оценку эффективности
/// </summary>
/// <remarks>
/// Используется для количественной оценки
/// продуктивности работы
/// </remarks>
public class ScorePointsResponse
{
    /// <summary>
    /// Балльная оценка эффективности
    /// </summary>
    /// <value>
    /// Числовая оценка в диапазоне от 0 до 10,
    /// где 0 - минимальная продуктивность,
    /// 10 - максимальная продуктивность
    /// </value>
    [JsonPropertyName("score")]
    public required double Score { get; init; }
}