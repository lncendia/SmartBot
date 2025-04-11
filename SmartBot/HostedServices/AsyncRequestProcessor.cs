using System.Threading.Channels;
using MediatR;
using SmartBot.Abstractions.Interfaces.Utils;

namespace SmartBot.HostedServices;

/// <summary>
/// Фоновый сервис для асинхронной обработки запросов (команд) через Channel.
/// Реализует паттерн "Производитель-Потребитель" (Producer-Consumer) с поддержкой многопоточной обработки.
/// </summary>
public sealed class AsyncRequestProcessor : BackgroundService, IAsyncSender
{
    /// <summary>
    /// Канал (Channel) для передачи запросов между производителями (producers) и потребителями (consumers).
    /// Использует ограниченную емкость (BoundedChannel) для контроля нагрузки.
    /// </summary>
    private readonly Channel<IRequest> _commandChannel;

    /// <summary>
    /// Провайдер служб (IServiceProvider) для создания новых областей (scopes) при обработке запросов.
    /// Позволяет корректно работать с зависимостями с ограниченным временем жизни (scoped services).
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Логгер для записи информации о работе процессора и ошибках.
    /// </summary>
    private readonly ILogger<AsyncRequestProcessor> _logger;

    /// <summary>
    /// Количество рабочих задач (воркеров), которые параллельно обрабатывают запросы из канала.
    /// </summary>
    private readonly int _workerCount;

    /// <summary>
    /// Инициализирует новый экземпляр класса AsyncRequestProcessor.
    /// </summary>
    /// <param name="serviceProvider">Провайдер служб для создания областей (scopes).</param>
    /// <param name="logger">Логгер для записи событий.</param>
    /// <param name="workerCount">Количество параллельных воркеров (по умолчанию: 5).</param>
    /// <param name="capacity">Максимальная емкость канала (по умолчанию: 100).</param>
    public AsyncRequestProcessor(
        IServiceProvider serviceProvider,
        ILogger<AsyncRequestProcessor> logger,
        int workerCount = 5,
        int capacity = 100
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _workerCount = workerCount;

        // Настройка канала с ограниченной емкостью.
        // При переполнении канала записывающие операции будут ждать (Wait) освобождения места.
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        _commandChannel = Channel.CreateBounded<IRequest>(options);
    }

    /// <summary>
    /// Запускает фоновую задачу обработки запросов.
    /// Создает указанное количество воркеров (_workerCount), каждый из которых независимо обрабатывает команды.
    /// </summary>
    /// <param name="stoppingToken">Токен отмены для graceful shutdown.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerTasks = new List<Task>();

        stoppingToken.Register(() => _logger.LogError("\n\nКакого хуя"));

        // Запуск N воркеров для параллельной обработки команд.
        for (var i = 0; i < _workerCount; i++)
        {
            workerTasks.Add(Task.Run(() => ProcessCommandsAsync(stoppingToken), stoppingToken));
        }

        // Ожидание завершения всех воркеров (происходит при остановке приложения).
        await Task.WhenAll(workerTasks);
    }

    /// <summary>
    /// Метод обработки команд, выполняемый каждым воркером.
    /// Читает команды из канала и выполняет их через ISender.
    /// </summary>
    /// <param name="stoppingToken">Токен отмены для graceful shutdown.</param>
    private async Task ProcessCommandsAsync(CancellationToken stoppingToken)
    {
        // Асинхронное чтение команд из канала до его завершения или отмены.
        await foreach (var command in _commandChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Создание новой области (scope) для обработки команды.
                // Важно для scoped-сервисов (например, DbContext).
                using var scope = _serviceProvider.CreateScope();

                // Получение отправителя (ISender) из DI-контейнера.
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                // Выполнение команды.
                await sender.Send(command, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Логирование ошибки без прерывания работы воркера.
                _logger.LogError(ex, "Ошибка при обработке команды {Command}", command.GetType().Name);
            }
        }
    }

    /// <summary>
    /// Добавляет запрос в очередь на обработку (реализация IAsyncSender).
    /// Используется клиентами для постановки команд в очередь.
    /// </summary>
    /// <param name="request">Запрос для обработки.</param>
    /// <param name="ct">Токен отмены.</param>
    public async Task Send(IRequest request, CancellationToken ct = default)
    {
        // Асинхронная запись команды в канал.
        await _commandChannel.Writer.WriteAsync(request, ct);
    }
}