using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.ResponseModels;

/// <summary>
/// Класс, представляющий ответ анализа отчета
/// </summary>
/// <remarks>
/// Содержит результаты обработки и оценки исходного отчета
/// </remarks>
public class ReportAnalysisResponse
{
    /// <summary>
    /// Оценка качества отчета по шкале от 1 до 10
    /// </summary>
    /// <value>
    /// Числовое значение от 1 (минимальное) до 10 (максимальное),
    /// где 1 - очень плохой отчет, 10 - идеальный отчет
    /// </value>
    [JsonPropertyName("score")]
    public required double Score { get; init; }

    /// <summary>
    /// Оптимизированная версия исходного отчета
    /// </summary>
    /// <value>
    /// Текст отчета после редактирования и улучшения,
    /// содержащий исправленные формулировки и структуру
    /// </value>
    [JsonPropertyName("edit")]
    public required string Edit { get; init; }
}