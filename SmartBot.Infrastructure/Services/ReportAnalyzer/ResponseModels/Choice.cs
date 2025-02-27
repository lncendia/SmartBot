using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.ResponseModels;

/// <summary>
/// Класс, представляющий выбор (ответ) от API.
/// </summary>
public class Choice
{
    /// <summary>
    /// Логарифмические вероятности (если доступны).
    /// </summary>
    [JsonPropertyName("logprobs")]
    public object? Logprobs { get; init; }

    /// <summary>
    /// Причина завершения генерации.
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public required string FinishReason { get; init; }

    /// <summary>
    /// Нативная причина завершения (если доступна).
    /// </summary>
    [JsonPropertyName("native_finish_reason")]
    public required string NativeFinishReason { get; init; }

    /// <summary>
    /// Индекс выборки.
    /// </summary>
    [JsonPropertyName("index")]
    public required int Index { get; init; }

    /// <summary>
    /// Сообщение, содержащее ответ.
    /// </summary>
    [JsonPropertyName("message")]
    public required MessageContent Message { get; init; }
}