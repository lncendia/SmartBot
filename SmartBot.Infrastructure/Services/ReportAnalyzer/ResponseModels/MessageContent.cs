using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.ResponseModels;

/// <summary>
/// Класс, представляющий содержимое сообщения.
/// </summary>
public class MessageContent
{
    /// <summary>
    /// Роль отправителя сообщения (например, "assistant").
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    /// <summary>
    /// Содержимое сообщения.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    /// <summary>
    /// Информация об отказе (если доступна).
    /// </summary>
    [JsonPropertyName("refusal")]
    public object? Refusal { get; init; }
}