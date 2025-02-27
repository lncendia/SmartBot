using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.ResponseModels;

/// <summary>
/// Класс, представляющий использование токенов.
/// </summary>
public class Usage
{
    /// <summary>
    /// Количество токенов, использованных в запросе.
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public required int PromptTokens { get; init; }

    /// <summary>
    /// Количество токенов, использованных в ответе.
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public required int CompletionTokens { get; init; }

    /// <summary>
    /// Общее количество токенов.
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public required int TotalTokens { get; init; }
}