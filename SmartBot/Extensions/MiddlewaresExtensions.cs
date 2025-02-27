using SmartBot.Middlewares;

namespace SmartBot.Extensions;

/// <summary>
/// Расширяющий метод для добавления промежуточных обработчиков в приложение.
/// </summary>
public static class MiddlewaresExtensions
{
    /// <summary>
    /// Добавляет промежуточный обработчик для верификации секретного токена.
    /// </summary>
    /// <param name="app">Строитель приложения.</param>
    public static void UseSecretToken(this IApplicationBuilder app)
    {
        // Получаем конфигурацию приложения
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();

        // Получаем секретный токен для верификации запросов вебхука
        var secretToken = configuration.GetRequiredValue<string>("Telegram:Webhook:SecretToken");

        // Добавляем промежуточный обработчик для верификации секретного токена
        app.UseMiddleware<SecretTokenMiddleware>(secretToken);
    }
}