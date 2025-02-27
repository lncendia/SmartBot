using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.RequestModels;

/// <summary>
/// Класс, представляющий JSON-схему.
/// </summary>
public class JsonSchema
{
    /// <summary>
    /// Имя схемы.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Флаг, указывающий на строгость схемы.
    /// </summary>
    [JsonPropertyName("strict")]
    public required bool Strict { get; init; }

    /// <summary>
    /// Схема объекта.
    /// </summary>
    [JsonPropertyName("schema")]
    public required Schema Schema { get; init; }
}