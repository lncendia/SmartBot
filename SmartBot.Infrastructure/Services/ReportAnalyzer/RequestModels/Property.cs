using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.RequestModels;

/// <summary>
/// Класс, представляющий отдельное свойство.
/// </summary>
public class Property
{
    /// <summary>
    /// Тип свойства.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Описание свойства.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }
}