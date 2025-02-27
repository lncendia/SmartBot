using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.RequestModels;

/// <summary>
/// Класс, представляющий свойства объекта.
/// </summary>
public class Properties
{
    /// <summary>
    /// Свойство, представляющее оценку.
    /// </summary>
    [JsonPropertyName("score")]
    public required Property Score { get; init; }

    /// <summary>
    /// Свойство, представляющее отредактированный отчёт.
    /// </summary>
    [JsonPropertyName("edit")]
    public required Property Edit { get; init; }
}