using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Extensions;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Infrastructure.Contexts;

namespace SmartBot.HostedServices;

/// <summary>
/// Фоновый сервис для очистки устаревших отчётов.
/// </summary>
/// <param name="serviceProvider">Провайдер сервисов для создания областей (scopes).</param>
/// <param name="dateTimeProvider">Провайдер даты и времени для работы с временными данными.</param>
/// <param name="logger">Логгер для записи событий и ошибок.</param>
public class ClearingHostedService(
    IServiceProvider serviceProvider,
    IDateTimeProvider dateTimeProvider,
    ILogger<ClearingHostedService> logger)
    : IHostedService, IDisposable
{
    /// <summary>
    /// Таймер для запуска задачи очистки устаревших отчётов.
    /// </summary>
    private Timer? _timer;

    /// <summary>
    /// Метод, который запускается при старте приложения.
    /// Настраивает таймер для запуска задачи очистки устаревших отчётов каждый день в 00:00.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Получаем текущее время
        var now = dateTimeProvider.Now;

        // Вычисляем время до следующего запуска (00:00 следующего дня)
        var nextRunTime = now.Date.AddDays(1);

        // Вычисляем интервал до следующего запуска
        var timeUntilNextRun = nextRunTime - now;

        // Создаем таймер, который запускает метод ClearOldReports в указанное время
        _timer = new Timer(ClearOldReports, null, timeUntilNextRun, TimeSpan.FromDays(1));

        // Возвращаем завершенный Task, так как метод синхронный
        return Task.CompletedTask;
    }

    /// <summary>
    /// Метод, который запускается при остановке приложения.
    /// Останавливает таймер.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Останавливаем таймер
        _timer?.Change(Timeout.Infinite, 0);

        // Возвращаем завершенный Task
        return Task.CompletedTask;
    }

    /// <summary>
    /// Метод для очистки устаревших отчётов.
    /// </summary>
    /// <param name="state">Состояние (не используется).</param>
    private async void ClearOldReports(object? state)
    {
        try
        {
            // Если сегодня выходной день, очистка не выполняется
            if (dateTimeProvider.Now.IsPreviousDayWeekend()) return;

            // Создаем область (scope) для работы с зависимостями (DI)
            using var scope = serviceProvider.CreateScope();

            // Получаем экземпляр ApplicationDbContext для работы с базой данных
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Вычисляем дату, старше которой отчёты считаются устаревшими (3 дня назад)
            var maxTime = dateTimeProvider.Now.Date.AddDays(-3);

            // Выполняем SQL-запрос для удаления устаревших отчётов
            var deletedCount = await context.Database.ExecuteSqlRawAsync(
                "DELETE FROM Reports WHERE Date < {0}", 
                maxTime
            );

            // Логируем результат очистки
            logger.LogInformation("{DeletedCount} of outdated reports has been deleted.", deletedCount);
        }
        catch (Exception ex)
        {
            // Логируем ошибку, если что-то пошло не так
            logger.LogError(ex, "An error occurred when clearing outdated reports.");
        }
    }

    /// <summary>
    /// Освобождает ресурсы, используемые таймером.
    /// </summary>
    public void Dispose()
    {
        // Освобождаем ресурсы таймера
        _timer?.Dispose();

        // Подавляем финализацию, так как ресурсы уже освобождены
        GC.SuppressFinalize(this);
    }
}