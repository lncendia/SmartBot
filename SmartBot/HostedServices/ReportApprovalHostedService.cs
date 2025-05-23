using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Extensions;
using SmartBot.Abstractions.Interfaces.Notification;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Interfaces.Utils;
using SmartBot.Abstractions.Models.Reports;

namespace SmartBot.HostedServices;

/// <summary>
/// Фоновый сервис для автоматического принятия отчётов по истечении времени ожидания.
/// </summary>
/// <param name="serviceProvider">Провайдер сервисов для создания областей (scopes).</param>
/// <param name="dateTimeProvider">Провайдер даты и времени для работы с временными данными.</param>
/// <param name="logger">Логгер для записи событий и ошибок.</param>
public class ReportApprovalHostedService(
    IServiceProvider serviceProvider,
    IDateTimeProvider dateTimeProvider,
    ILogger<ReportApprovalHostedService> logger)
    : IHostedService, IDisposable
{
    /// <summary>
    /// Таймер для запуска задачи автоматического принятия отчётов по истечении времени ожидания.
    /// </summary>
    private Timer? _timer;

    /// <summary>
    /// Метод, который запускается при старте приложения.
    /// Настраивает таймер для запуска задачи автоматического принятия отчётов по истечении времени ожидания.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Получаем текущее время
        var now = dateTimeProvider.Now;

        // Вычисляем время следующего запуска (следующие 30-минутные отметки)
        var minutes = now.Minute;
        var nextRunMinutes = 30 - minutes % 30;
        var nextRunTime = now.AddMinutes(nextRunMinutes).AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
    
        // Вычисляем интервал до следующего запуска
        var timeUntilNextRun = nextRunTime - now;
    
        // Создаем таймер, который запускает метод ApproveReports каждые 30 минут
        // Первый запуск - в следующую 30-минутную отметку
        // Последующие запуски - каждые 30 минут
        _timer = new Timer(
            callback: ApproveReports,
            state: null,
            dueTime: timeUntilNextRun,
            period: TimeSpan.FromMinutes(30));

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
    /// Метод для автоматического принятия отчётов по истечении времени ожидания
    /// </summary>
    /// <param name="state">Параметр состояния (не используется в текущей реализации)</param>
    private async void ApproveReports(object? state)
    {
        try
        {
            // Проверяем, находится ли текущее время в рабочем периоде
            if (!dateTimeProvider.Now.IsWorkingPeriod()) return;

            // Определяем, является ли текущий период вечерним (для обработки соответствующих отчётов)
            var isEveningPeriod = dateTimeProvider.Now.IsEveningPeriod();

            // Устанавливаем временную метку (текущее время минус 1 час) для отчётов
            var timeMark = dateTimeProvider.Now.AddHours(-1);

            // Создаём область видимости для работы с зависимостями
            using var scope = serviceProvider.CreateScope();

            // Инициализируем Unit of Work для работы с базой данных
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            
            // Инициализируем ISender для отправки команд
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

            // Получаем сервис уведомлений
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            // Формируем базовый запрос для получения отчётов
            var query = uow.Query<Report>()
                
                // Включаем информацию о пользователе для последующих уведомлений
                .Include(r => r.User).AsQueryable();

            // В зависимости от периода добавляем соответствующие условия фильтрации
            if (isEveningPeriod)
            {
                // Для вечернего периода проверяем вечерние отчёты
                query = query.Where(r =>
                    !r.EveningReport!.Approved &&
                    !r.EveningReport.ApprovedBySystem &&
                    r.EveningReport.Date <= timeMark);
            }
            else
            {
                // Для утреннего периода проверяем утренние отчёты
                query = query.Where(r =>
                    !r.MorningReport.Approved &&
                    !r.MorningReport.ApprovedBySystem &&
                    r.MorningReport.Date <= timeMark);
            }

            // Выполняем запрос и получаем список отчётов
            var reports = await query.ToArrayAsync();

            // Обрабатываем каждый найденный отчёт
            foreach (var report in reports)
            {
                // Помечаем отчёт как автоматически принятый системой
                report.GetReport(isEveningPeriod)!.ApprovedBySystem = true;
            }

            // Сохраняем все изменения в базе данных
            await uow.SaveChangesAsync();
            
            // Логгируем
            logger.LogInformation("Successfully automatically accepted {ApprovedCount} reports", reports.Length);
            
            // Обрабатываем каждый отчет в коллекции
            foreach (var report in reports)
            {
                // Создаем команду для автоматического принятия отчета
                var command = new AutomaticApproveReportCommand
                {
                    // Передаем текущий отчет для обработки
                    Report = report,
        
                    // Указываем тип отчета (утренний/вечерний)
                    EveningReport = isEveningPeriod
                };
    
                // Отправляем команду на выполнение через медиатор
                await mediator.Send(command);
            }
        }
        catch (Exception ex)
        {
            // Логируем критические ошибки в процессе выполнения
            logger.LogError(ex, "Critical error in automatic report acceptance");
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