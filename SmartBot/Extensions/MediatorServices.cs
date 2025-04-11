using SmartBot.Abstractions.Interfaces.Utils;
using SmartBot.HostedServices;
using SmartBot.Services.CommandHandlers;

namespace SmartBot.Extensions;

/// <summary>
/// Статический класс для регистрации сервисов, связанных с MediatR и обработкой команд
/// </summary>
public static class MediatorServices
{
    /// <summary>
    /// Регистрирует сервисы MediatR и настраивает систему обработки команд
    /// </summary>
    /// <param name="services">Коллекция служб DI-контейнера</param>
    /// <param name="configuration">Конфигурация приложения</param>
    /// <remarks>
    /// Метод выполняет:
    /// 1. Регистрацию MediatR с автоматическим сканированием обработчиков команд
    /// 2. Настройку фонового сервиса для обработки асинхронных команд
    /// 3. Регистрацию IAsyncSender для постановки команд в очередь
    /// </remarks>
    public static void AddMediatorServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Получаем настройки очереди из конфигурации
        var asyncQueueCapacity = configuration.GetRequiredValue<int>("AsyncQueue:Capacity");
        var asyncQueueWorkersCount = configuration.GetRequiredValue<int>("AsyncQueue:WorkersCount");

        // Регистрация MediatR с автоматическим обнаружением обработчиков команд
        services.AddMediatR(mediatrConfiguration =>
        {
            // Сканируем сборку, содержащую StartCommandHandler, для поиска всех обработчиков
            mediatrConfiguration.RegisterServicesFromAssembly(typeof(StartCommandHandler).Assembly);
        });

        // Регистрация фонового сервиса обработки команд
        services.AddHostedService(sp =>
        {
            // Получаем экземпляр логгера из DI-контейнера
            var logger = sp.GetRequiredService<ILogger<AsyncRequestProcessor>>();

            // Создаем экземпляр AsyncRequestProcessor с параметрами из конфигурации
            return new AsyncRequestProcessor(
                sp, // IServiceProvider
                logger, // ILogger
                asyncQueueWorkersCount, // Количество рабочих потоков
                asyncQueueCapacity // Емкость очереди
            );
        });
        
        // Регистрация IAsyncSender как синглтона (использует тот же экземпляр AsyncRequestProcessor)
        services.AddSingleton<IAsyncSender>(sp => sp.GetServices<IHostedService>()
            .OfType<AsyncRequestProcessor>()
            .First());
    }
}