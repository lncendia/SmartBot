using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.RequestModels;

/// <summary>
/// Класс, представляющий сообщение.
/// </summary>
public class Message
{
    /// <summary>
    /// Роль отправителя сообщения (например, система или пользователь).
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    /// <summary>
    /// Содержимое сообщения.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}