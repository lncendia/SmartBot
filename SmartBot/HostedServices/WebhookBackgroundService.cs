using Telegram.Bot;
using Telegram.Bot.Types;

namespace SmartBot.HostedServices;

/// <summary>
/// Фоновый сервис для установки вебхука при запуске приложения.
/// </summary>
/// <param name="serviceProvider">Провайдер DI.</param>
/// <param name="settings">Настройки вебхука.</param>
/// <param name="settings">Логгер.</param>
public class WebhookBackgroundService(
    IServiceProvider serviceProvider,
    WebhookSettings settings,
    ILogger<WebhookBackgroundService> logger) : BackgroundService
{
    /// <summary>
    /// Основной метод выполнения фоновой задачи.
    /// </summary>
    /// <param name="stoppingToken">Токен для отмены операции.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Создаем область провайдера DI
        using var scope = serviceProvider.CreateScope();

        // Получаем клиент TelegramBot
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        // Проверяем, указан ли путь к сертификату
        if (string.IsNullOrEmpty(settings.CertificatePath))
        {
            // Если сертификат не указан, устанавливаем вебхук без сертификата
            await botClient.SetWebhook(
                url: settings.WebhookUrl, // URL вебхука
                secretToken: settings.SecretToken, // Секретный токен
                cancellationToken: stoppingToken // Токен отмены
            );
        }
        else
        {
            // Если сертификат указан, открываем поток для чтения сертификата
            await using var certificateStream =
                new FileStream(settings.CertificatePath, FileMode.Open, FileAccess.Read);

            // Устанавливаем вебхук с использованием сертификата и секретного токена
            await botClient.SetWebhook(
                url: settings.WebhookUrl, // URL вебхука
                certificate: new InputFileStream(certificateStream,
                    Path.GetFileName(settings.CertificatePath)), // Сертификат
                secretToken: settings.SecretToken, // Секретный токен
                cancellationToken: stoppingToken // Токен отмены
            );
        }

        // Логируем успешную установку вебхука без сертификата
        logger.LogInformation("The webhook has been successfully installed on the URL: {webhookUrl}",
            settings.WebhookUrl);
    }
}