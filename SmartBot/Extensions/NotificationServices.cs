using SmartBot.Abstractions.Interfaces;
using SmartBot.HostedServices;
using SmartBot.Services.Services;

namespace SmartBot.Extensions;

/// <summary>
/// Статический класс сервисов уведомлений.
/// </summary>
public static class NotificationServices
{
    /// <summary>
    /// Добавляет сервисы уведомлений в коллекцию служб.
    /// </summary>
    /// <param name="services">Коллекция служб, в которую будут добавлены новые сервисы.</param>
    public static void AddNotificationServices(this IServiceCollection services)
    {
        // Добавляем сервис уведомлений
        services.AddScoped<INotificationService, NotificationService>();

        // Добавляем хост-сервис уведомлений
        services.AddHostedService<NotificationHostedService>();
    }
}