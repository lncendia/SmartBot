using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.ResponseModels;

/// <summary>
/// Основной класс, представляющий отчёт.
/// </summary>
public class Report
{
    /// <summary>
    /// Оценка исходного отчёта от 1 до 5.
    /// </summary>
    [JsonPropertyName("score")]
    public required double Score { get; init; }

    /// <summary>
    /// Отредактированный отчёт.
    /// </summary>
    [JsonPropertyName("edit")]
    public required string Edit { get; init; }
}