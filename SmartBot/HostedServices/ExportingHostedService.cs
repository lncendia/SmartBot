using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Extensions;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Interfaces.DataExporter;
using SmartBot.Abstractions.Models;

namespace SmartBot.HostedServices;

/// <summary>
/// Фоновый сервис для экспорта отчётов.
/// </summary>
/// <param name="serviceProvider">Провайдер сервисов для создания областей (scopes).</param>
/// <param name="dateTimeProvider">Провайдер даты и времени для работы с временными данными.</param>
/// <param name="logger">Логгер для записи событий и ошибок.</param>
public class ExportingHostedService(
    IServiceProvider serviceProvider,
    IDateTimeProvider dateTimeProvider,
    ILogger<NotificationHostedService> logger)
    : IHostedService, IDisposable
{
    /// <summary>
    /// Таймер для запуска задачи экспорта отчётов.
    /// </summary>
    private Timer? _timer;

    /// <summary>
    /// Метод, который запускается при старте приложения.
    /// Настраивает таймер для запуска задачи экспорта отчётов каждый день в 20:00.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Получаем текущее время
        var now = dateTimeProvider.Now;

        // Вычисляем время до следующего запуска (20:00 сегодня или завтра)
        var nextRunTime = now.TimeOfDay >= TimeSpan.FromHours(20)
            ? now.Date.AddDays(1).AddHours(20) // Если уже позже 20:00, запускаем завтра
            : now.Date.AddHours(20); // Иначе запускаем сегодня в 20:00

        // Вычисляем интервал до следующего запуска
        var timeUntilNextRun = nextRunTime - now;

        // Создаем таймер, который запускает метод ExportReports в указанное время
        _timer = new Timer(ExportReports, null, timeUntilNextRun, TimeSpan.FromDays(1));

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
    /// Метод для экспорта отчётов.
    /// </summary>
    /// <param name="state">Состояние (не используется).</param>
    private async void ExportReports(object? state)
    {
        try
        {
            // Если сегодня выходной день, то не экспортируем отчёты.
            if (dateTimeProvider.Now.IsWeekend()) return;

            // Создаем область (scope) для работы с зависимостями (DI)
            using var scope = serviceProvider.CreateScope();

            // Получаем экземпляр UnitOfWork для работы с базой данных
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Получаем экземпляр сервиса для экспорта данных
            // var exporter = scope.ServiceProvider.GetRequiredService<IDataExporter>();

            // Получаем данные о последнем экспорте из базы данных
            var exporterData = await unitOfWork.Query<Exporter>().SingleOrDefaultAsync();

            // Переменная для хранения даты последнего экспортированного отчёта
            DateTime lastExportedDate;

            // Если данные о последнем экспорте отсутствуют
            if (exporterData == null)
            {
                // Устанавливаем дату последнего экспорта на текущую дату
                lastExportedDate = dateTimeProvider.Now.Date;

                // Создаем новый объект для хранения данных экспорта
                exporterData = new Exporter();

                // Добавляем новый объект в базу данных
                await unitOfWork.AddAsync(exporterData);
            }
            else
            {
                // Получаем дату последнего экспортированного отчёта
                var lastDate = await unitOfWork

                    // Запрашиваем таблицу отчётов
                    .Query<Report>()

                    // Фильтруем по ID последнего экспортированного отчёта
                    .Where(r => r.Id == exporterData.LastExportedReportId)

                    // Выбираем дату отчёта
                    .Select(r => r.Date)

                    // Получаем первую запись или значение по умолчанию
                    .FirstOrDefaultAsync();

                // Если дата последнего экспорта не найдена, используем текущую дату
                lastExportedDate = lastDate == default ? dateTimeProvider.Now.Date : lastDate;
            }

            // Получаем список отчётов для экспорта
            var reports = await unitOfWork.Query<User>()

                // Выполняем LEFT JOIN между таблицами User и Report
                .GroupJoin(
                    // Запрашиваем таблицу отчётов
                    unitOfWork.Query<Report>(),

                    // Ключ для соединения: ID пользователя
                    user => user.Id,

                    // Ключ для соединения: ID пользователя в отчёте
                    report => report.UserId,

                    // Результат соединения: объект с пользователем и его отчётами
                    (user, reports) => new { User = user, Reports = reports }
                )

                // Разворачиваем группировку и применяем LEFT JOIN
                .SelectMany(
                    // Для каждой группы пользователя и его отчётов
                    userGroup => userGroup.Reports.DefaultIfEmpty(),

                    // Создаем объект ReportData для каждого отчёта (или null, если отчёта нет)
                    (userGroup, report) => new ReportData
                    {
                        // Имя пользователя
                        Name = userGroup.User.FullName,

                        // Должность пользователя
                        Position = userGroup.User.Position,

                        // Дата отчёта (если отчёт есть, иначе текущая дата)
                        Date = report != null ? report.Date.Date : dateTimeProvider.Now.Date,

                        // Утренний отчёт
                        MorningReport = report != null ? report.MorningReport : null,

                        // Вечерний отчёт
                        EveningReport = report != null ? report.EveningReport : null,

                        // Комментарий к отчёту
                        Comment = report != null ? report.Comment : null,

                        // ID отчёта
                        Id = report != null ? report.Id : null,
                    }
                )

                // Фильтруем отчёты по дате (только те, которые новее последнего экспорта)
                .Where(r => r.Date >= lastExportedDate)

                // Сортируем отчёты по дате в порядке убывания
                .OrderByDescending(r => r.Date)

                // Получаем список отчётов
                .ToListAsync();

            // Экспортируем отчёты с помощью сервиса экспорта
            // await exporter.ExportReportsAsync(reports);

            // Обновляем ID последнего экспортированного отчёта
            exporterData.LastExportedReportId = reports.FirstOrDefault(r => r.Id.HasValue)?.Id;

            // Обновляем дату последнего экспорта
            exporterData.LastExportingDate = dateTimeProvider.Now;

            // Сохраняем изменения в базе данных
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Логируем ошибку, если что-то пошло не так
            logger.LogError(ex, "Ошибка при экспорте отчётов.");
        }
    }

    /// <summary>
    /// Метод для освобождения ресурсов
    /// </summary>
    public void Dispose()
    {
        // Освобождаем ресурсы таймера
        _timer?.Dispose();

        // Подавляем финализацию, так как ресурсы уже освобождены
        GC.SuppressFinalize(this);
    }
}