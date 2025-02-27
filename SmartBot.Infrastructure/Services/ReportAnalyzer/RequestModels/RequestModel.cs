using System.Text.Json.Serialization;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer.RequestModels;

/// <summary>
/// Класс, представляющий модель запроса.
/// </summary>
public class RequestModel
{
    /// <summary>
    /// Модель, используемая для формирования отчёта.
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>
    /// Сообщения, содержащие инструкции и данные для формирования отчёта.
    /// </summary>
    [JsonPropertyName("messages")]
    public required Message[] Messages { get; init; }

    /// <summary>
    /// Формат ответа.
    /// </summary>
    [JsonPropertyName("response_format")]
    public required ResponseFormat ResponseFormat { get; init; }
}