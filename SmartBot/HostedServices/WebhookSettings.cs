namespace SmartBot.HostedServices;

/// <summary>
/// Структура для хранения настроек вебхука.
/// </summary>
public class WebhookSettings
{
    /// <summary>
    /// URL, на который Telegram будет отправлять запросы.
    /// </summary>
    public required string WebhookUrl { get; init; }

    /// <summary>
    /// Путь к самоподписному сертификату для защиты вебхука.
    /// </summary>
    public string? CertificatePath { get; init; }

    /// <summary>
    /// Секретный токен для верификации запросов от Telegram.
    /// </summary>
    public required string SecretToken { get; init; }
}