using SmartBot.Abstractions.Interfaces;
using SmartBot.HostedServices;
using SmartBot.Services.Services;
using Telegram.Bot;
using IUpdateHandler = SmartBot.Abstractions.Interfaces.IUpdateHandler;

namespace SmartBot.Extensions;

/// <summary>
/// Статический класс для регистрации сервисов, связанных с Telegram.
/// </summary>
public static class TelegramServices
{
    /// <summary>
    /// Метод расширения для добавления сервисов, связанных с Telegram, в коллекцию служб.
    /// </summary>
    /// <param name="services">Коллекция служб, в которую будут добавлены новые сервисы.</param>
    /// <param name="configuration">Конфигурация приложения, содержащая параметры подключения и пути к хранилищам.</param>
    public static void AddTelegramServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Получаем токен бота из конфигурации
        var token = configuration.GetRequiredValue<string>("Telegram:Token");

        // Создаем объект настроек вебхука
        var webhookSettings = new WebhookSettings
        {
            // Получаем URL вебхука из конфигурации
            WebhookUrl = configuration.GetRequiredValue<string>("Telegram:Webhook:Url"),

            // Получаем путь к сертификату из конфигурации (может быть null, если не используется)
            CertificatePath = configuration.GetValue<string>("Telegram:Webhook:Cert"),

            // Получаем секретный токен для верификации запросов вебхука
            SecretToken = configuration.GetRequiredValue<string>("Telegram:Webhook:SecretToken")
        };

        // Регистрируем HttpClient с именем "TgWebhook" для использования в TelegramBotClient
        services.AddHttpClient("TgWebhook")
            
            // Регистрируем типизированный клиент ITelegramBotClient
            .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(token, httpClient));

        // Настройте ASP.NET Json десериализацию для типов Telegram.Bot
        services.ConfigureTelegramBotMvc();
        
        // Регистрируем обработчик обновлений (IUpdateHandler) как Scoped-сервис
        services.AddScoped<IUpdateHandler, UpdateHandler>();

        // Регистрируем фабрику команд для обработки текстовых сообщений как Singleton
        services.AddSingleton<IMessageCommandFactory, MessageCommandFactory>();

        // Регистрируем фабрику команд для обработки callback-запросов как Singleton
        services.AddSingleton<ICallbackQueryCommandFactory, CallbackQueryCommandFactory>();

        // Регистрируем провайдер времени (IDateTimeProvider) как Singleton
        services.AddSingleton<IDateTimeProvider, MoscowDateTimeProvider>();

        // Регистрируем сервис синхронизации действий пользователей
        services.AddSingleton<IUserSynchronizationService, UserSynchronizationService>();

        // Регистрируем настройки вебхука
        services.AddSingleton(webhookSettings);

        // Регистрируем фоновый сервис для настройки вебхука
        services.AddHostedService<WebhookBackgroundService>();
    }
}