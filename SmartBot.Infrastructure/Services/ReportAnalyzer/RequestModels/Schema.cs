using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.RequestModels;

/// <summary>
/// Класс, представляющий схему объекта.
/// </summary>
public class Schema
{
    /// <summary>
    /// Тип объекта.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Свойства объекта.
    /// </summary>
    [JsonPropertyName("properties")]
    public required Properties Properties { get; init; }

    /// <summary>
    /// Обязательные свойства объекта.
    /// </summary>
    [JsonPropertyName("required")]
    public required string[] Required { get; init; }

    /// <summary>
    /// Флаг, указывающий на наличие дополнительных свойств.
    /// </summary>
    [JsonPropertyName("additionalProperties")]
    public required bool AdditionalProperties { get; init; }
}