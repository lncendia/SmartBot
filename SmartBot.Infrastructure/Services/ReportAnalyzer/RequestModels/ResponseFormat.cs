using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.RequestModels;

/// <summary>
/// Класс, представляющий формат ответа.
/// </summary>
public class ResponseFormat
{
    /// <summary>
    /// Тип формата ответа.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Схема JSON для ответа.
    /// </summary>
    [JsonPropertyName("json_schema")]
    public required JsonSchema JsonSchema { get; init; }
}