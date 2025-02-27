using SmartBot.Abstractions.Extensions;
using SmartBot.Abstractions.Interfaces;

namespace SmartBot.HostedServices;

/// <summary>
/// Класс для запуска сервиса уведомлений о событиях, связанных с отчётами.
/// </summary>
/// <param name="serviceProvider">Провайдер сервисов.</param>
/// <param name="dateTimeProvider">Провайдер даты и времени.</param>
/// <param name="logger">Логгер.</param>
public class NotificationHostedService(
    IServiceProvider serviceProvider,
    IDateTimeProvider dateTimeProvider,
    ILogger<NotificationHostedService> logger)
    : IHostedService, IDisposable
{
    /// <summary>
    /// Таймер для уведомления о необходимости сдать утренний отчёт.
    /// </summary>
    private Timer? _morningReportTimer;

    /// <summary>
    /// Таймер для уведомления о том, что время сдачи утреннего отчёта подходит к концу.
    /// </summary>
    private Timer? _morningDeadlineTimer;

    /// <summary>
    /// Таймер для уведомления о том, что утренний отчёт не был сдан.
    /// </summary>
    private Timer? _morningMissedTimer;

    /// <summary>
    /// Таймер для уведомления о необходимости сдать вечерний отчёт.
    /// </summary>
    private Timer? _eveningReportTimer;

    /// <summary>
    /// Таймер для уведомления о том, что время сдачи вечернего отчёта подходит к концу.
    /// </summary>
    private Timer? _eveningDeadlineTimer;

    /// <summary>
    /// Таймер для уведомления о том, что вечерний отчёт не был сдан.
    /// </summary>
    private Timer? _eveningMissedTimer;

    /// <summary>
    /// Метод, который запускается при старте приложения.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Получаем текущее время.
        var now = dateTimeProvider.Now;

        // Устанавливаем таймер для утреннего уведомления (9:00).
        _morningReportTimer = CreateTimer(now, TimeSpan.FromHours(9), NotifyMorningReportDueAsync);

        // Устанавливаем таймер для уведомления о дедлайне утреннего отчёта (9:30).
        _morningDeadlineTimer =
            CreateTimer(now, TimeSpan.FromHours(9, 30), NotifyMorningReportDeadlineApproachingAsync);

        // Устанавливаем таймер для уведомления о несдаче утреннего отчёта (10:00).
        _morningMissedTimer = CreateTimer(now, TimeSpan.FromHours(10), NotifyMorningReportMissedAsync);

        // Устанавливаем таймер для вечернего уведомления (18:00).
        _eveningReportTimer = CreateTimer(now, TimeSpan.FromHours(18), NotifyEveningReportDueAsync);

        // Устанавливаем таймер для уведомления о дедлайне вечернего отчёта (18:30).
        _eveningDeadlineTimer =
            CreateTimer(now, TimeSpan.FromHours(18, 30), NotifyEveningReportDeadlineApproachingAsync);

        // Устанавливаем таймер для уведомления о несдаче вечернего отчёта (19:00).
        _eveningMissedTimer = CreateTimer(now, TimeSpan.FromHours(19), NotifyEveningReportMissedAsync);

        // Возвращаем завершенный Task, так как метод синхронный.
        return Task.CompletedTask;
    }

    /// <summary>
    /// Метод, который вызывается при остановке приложения.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Останавливаем все таймеры.
        _morningReportTimer?.Change(Timeout.Infinite, 0);
        _morningDeadlineTimer?.Change(Timeout.Infinite, 0);
        _morningMissedTimer?.Change(Timeout.Infinite, 0);
        _eveningReportTimer?.Change(Timeout.Infinite, 0);
        _eveningDeadlineTimer?.Change(Timeout.Infinite, 0);
        _eveningMissedTimer?.Change(Timeout.Infinite, 0);

        // Возвращаем завершенный Task, так как метод синхронный.
        return Task.CompletedTask;
    }

    /// <summary>
    /// Создает таймер, который срабатывает в указанное время каждый день.
    /// </summary>
    /// <param name="now">Текущее время.</param>
    /// <param name="timeOfDay">Время дня, когда должен сработать таймер.</param>
    /// <param name="callback">Метод, который будет вызван при срабатывании таймера.</param>
    /// <returns>Таймер.</returns>
    private Timer CreateTimer(DateTime now, TimeSpan timeOfDay, Func<Task> callback)
    {
        // Вычисляем время следующего срабатывания таймера.
        var nextRunTime = now.Date.Add(timeOfDay);

        // Если время запуска уже пропущено, то запускаем уже на следующий день.
        if (nextRunTime < now) nextRunTime = nextRunTime.AddDays(1);

        // Вычисляем интервал до следующего срабатывания.
        var dueTime = nextRunTime - now;

        // Создаем таймер, который срабатывает каждый день в указанное время.
        return new Timer(_ => ExecuteCallback(callback), null, dueTime, TimeSpan.FromDays(1));
    }

    /// <summary>
    /// Выполняет переданный callback и обрабатывает возможные ошибки.
    /// </summary>
    /// <param name="callback">Метод, который нужно выполнить.</param>
    private async void ExecuteCallback(Func<Task> callback)
    {
        try
        {
            // Если сегодня выходной день, то не отправляем уведомления.
            if (dateTimeProvider.Now.IsWeekend()) return;

            // Выполняем callback.
            await callback();
        }
        catch (Exception ex)
        {
            // Логируем ошибку, если что-то пошло не так.
            logger.LogError(ex, "Couldn't send notifications.");
        }
    }

    /// <summary>
    /// Уведомляет о необходимости сдать утренний отчёт.
    /// </summary>
    private async Task NotifyMorningReportDueAsync()
    {
        // Создаем область провайдера DI
        using var scope = serviceProvider.CreateScope();

        // Получаем сервис рассылки уведомлений
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        // Рассылаем уведомления
        await notificationService.NotifyMorningReportDueAsync();
    }

    /// <summary>
    /// Уведомляет о том, что время сдачи утреннего отчёта подходит к концу.
    /// </summary>
    private async Task NotifyMorningReportDeadlineApproachingAsync()
    {
        // Создаем область провайдера DI
        using var scope = serviceProvider.CreateScope();

        // Получаем сервис рассылки уведомлений
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        // Рассылаем уведомления
        await notificationService.NotifyMorningReportDeadlineApproachingAsync();
    }

    /// <summary>
    /// Уведомляет о том, что утренний отчёт не был сдан.
    /// </summary>
    private async Task NotifyMorningReportMissedAsync()
    {
        // Создаем область провайдера DI
        using var scope = serviceProvider.CreateScope();

        // Получаем сервис рассылки уведомлений
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        // Рассылаем уведомления
        await notificationService.NotifyMorningReportMissedAsync();
    }

    /// <summary>
    /// Уведомляет о необходимости сдать вечерний отчёт.
    /// </summary>
    private async Task NotifyEveningReportDueAsync()
    {
        // Создаем область провайдера DI
        using var scope = serviceProvider.CreateScope();

        // Получаем сервис рассылки уведомлений
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        // Рассылаем уведомления
        await notificationService.NotifyEveningReportDueAsync();
    }

    /// <summary>
    /// Уведомляет о том, что время сдачи вечернего отчёта подходит к концу.
    /// </summary>
    private async Task NotifyEveningReportDeadlineApproachingAsync()
    {
        // Создаем область провайдера DI
        using var scope = serviceProvider.CreateScope();

        // Получаем сервис рассылки уведомлений
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        // Рассылаем уведомления
        await notificationService.NotifyEveningReportDeadlineApproachingAsync();
    }

    /// <summary>
    /// Уведомляет о том, что вечерний отчёт не был сдан.
    /// </summary>
    private async Task NotifyEveningReportMissedAsync()
    {
        // Создаем область провайдера DI
        using var scope = serviceProvider.CreateScope();

        // Получаем сервис рассылки уведомлений
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        // Рассылаем уведомления
        await notificationService.NotifyEveningReportMissedAsync();
    }

    /// <summary>
    /// Метод для освобождения ресурсов.
    /// </summary>
    public void Dispose()
    {
        // Освобождаем ресурсы всех таймеров.
        _morningReportTimer?.Dispose();
        _morningDeadlineTimer?.Dispose();
        _morningMissedTimer?.Dispose();
        _eveningReportTimer?.Dispose();
        _eveningDeadlineTimer?.Dispose();
        _eveningMissedTimer?.Dispose();

        // Подавляем финализацию, так как ресурсы уже освобождены.
        GC.SuppressFinalize(this);
    }
}