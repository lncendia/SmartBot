using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.ResponseModels;

/// <summary>
/// Класс, представляющий ответ от OpenRouter API.
/// </summary>
public class OpenRouterResponse
{
    /// <summary>
    /// Уникальный идентификатор ответа.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Провайдер, предоставивший ответ.
    /// </summary>
    [JsonPropertyName("provider")]
    public required string Provider { get; init; }

    /// <summary>
    /// Модель, использованная для генерации ответа.
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>
    /// Тип объекта (например, "chat.completion").
    /// </summary>
    [JsonPropertyName("object")]
    public required string Object { get; init; }

    /// <summary>
    /// Временная метка создания ответа.
    /// </summary>
    [JsonPropertyName("created")]
    public required long Created { get; init; }

    /// <summary>
    /// Массив выборок (ответов), предоставленных API.
    /// </summary>
    [JsonPropertyName("choices")]
    public required Choice[] Choices { get; init; }

    /// <summary>
    /// Информация об использовании токенов.
    /// </summary>
    [JsonPropertyName("usage")]
    public required Usage Usage { get; init; }
}