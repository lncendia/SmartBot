using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using SmartBot.Abstractions.Interfaces.ReportAnalyzer;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer;

/// <summary>
/// Прокси-клиент с отказоустойчивостью для анализа отчетов.
/// Этот класс оборачивает существующую реализацию <see cref="IReportAnalyzer"/> и добавляет политики устойчивости,
/// такие как таймауты и повторные попытки, для обработки временных сбоев.
/// </summary>
public class ResilientAnalyzer : IReportAnalyzer
{
    /// <summary>
    /// Пайплайн для обработки отказоустойчивых запросов.
    /// Этот пайплайн включает политики, такие как таймаут и повторные попытки, для обеспечения надежного взаимодействия
    /// с базовым анализатором.
    /// </summary>
    private readonly ResiliencePipeline _pipeline;

    /// <summary>
    /// Базовый анализатор отчетов, который используется для выполнения операций анализа.
    /// </summary>
    private readonly IReportAnalyzer _analyzer;

    /// <summary>
    /// Конструктор класса.
    /// </summary>
    /// <param name="innerAnalyzer">Реализация интерфейса анализатора отчетов.</param>
    /// <param name="logger">Логгер для записи событий и ошибок.</param>
    public ResilientAnalyzer(IReportAnalyzer innerAnalyzer, ILogger<ResilientAnalyzer> logger)
    {
        // Устанавливаем базовый анализатор
        _analyzer = innerAnalyzer;

        // Настройка пайплайна отказоустойчивости
        _pipeline = new ResiliencePipelineBuilder()

            // Добавляем стратегию обработки таймаутов
            .AddTimeout(new TimeoutStrategyOptions
            {
                // Генератор таймаута: 40 секунд
                TimeoutGenerator = static _ => new ValueTask<TimeSpan>(TimeSpan.FromSeconds(40)),

                // Действие при срабатывании таймаута
                OnTimeout = _ =>
                {
                    // Логируем предупреждение о таймауте
                    logger.LogWarning("Report analyzer request timed out.");

                    // Возвращаем завершенную задачу
                    return ValueTask.CompletedTask;
                }
            })

            // Добавляем стратегию повторных попыток (Retry)
            .AddRetry(new RetryStrategyOptions
            {
                // Максимальное количество попыток
                MaxRetryAttempts = 4,

                // Базовая задержка между попытками
                Delay = TimeSpan.FromSeconds(5),

                // Условия для выполнения повторной попытки
                ShouldHandle = new PredicateBuilder()

                    // Обработка исключений типа HttpRequestException
                    .Handle<HttpRequestException>()

                    // Обработка исключений типа JsonException
                    .Handle<JsonException>(),

                // Действие при каждой повторной попытке
                OnRetry = _ =>
                {
                    // Логируем предупреждение о повторной попытке
                    logger.LogWarning("Failed to process the report. Retrying...");

                    // Возвращаем завершенную задачу
                    return ValueTask.CompletedTask;
                }
            })

            // Добавление стратегии "Circuit Breaker" (прерыватель цепи) в пайплайн отказоустойчивости
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                // Условие, при котором срабатывает Circuit Breaker
                ShouldHandle = new PredicateBuilder()

                    // Обработка исключений типа HttpRequestException
                    .Handle<HttpRequestException>()

                    // Обработка исключений типа JsonException
                    .Handle<JsonException>(),

                // Минимальная доля ошибок, при которой цепь будет разорвана
                // (если более 20% запросов завершились ошибкой, цепь разрывается)
                FailureRatio = 0.2,

                // Продолжительность временного окна для анализа частоты ошибок
                SamplingDuration = TimeSpan.FromMinutes(1),

                // Минимальное количество запросов, необходимое для анализа частоты ошибок
                MinimumThroughput = 10,

                // Продолжительность состояния "разорванной цепи" перед следующим запросом
                BreakDuration = TimeSpan.FromMinutes(3),

                // Действие, выполняемое при переходе цепи в состояние "разорвано"
                OnOpened = _ =>
                {
                    // Логируем предупреждение
                    logger.LogWarning("Too many failed requests to the report analyzer. Circuit breaker opened.");

                    // Возвращаем завершенную задачу
                    return ValueTask.CompletedTask;
                },

                // Действие, выполняемое при переходе цепи в состояние "полуоткрыто"
                OnHalfOpened = _ =>
                {
                    // Логируем информацию о контрольной попытке запроса
                    logger.LogInformation("Attempting a test request to the report analyzer (half-open state).");

                    // Возвращаем значение по умолчанию
                    return ValueTask.CompletedTask;
                },

                // Действие, выполняемое при переходе цепи в состояние "закрыто"
                OnClosed = _ =>
                {
                    // Логируем информацию о восстановлении подключения
                    logger.LogInformation("Report analyzer connection restored. Circuit breaker closed.");

                    // Возвращаем завершенную задачу
                    return ValueTask.CompletedTask;
                }
            })

            // Завершаем настройку пайплайна
            .Build();
    }

    /// <inheritdoc/>
    /// <summary>
    /// Анализирует отчет с использованием отказоустойчивого пайплайна.
    /// </summary>
    public async Task<ReportAnalyzeResult> AnalyzeAsync(string report, CancellationToken cancellationToken = default)
    {
        // Выполняем запрос через пайплайн с отказоустойчивостью
        var result = await _pipeline.ExecuteAsync(
            async ct => await _analyzer.AnalyzeAsync(report, ct),
            cancellationToken
        );

        // Возвращаем результат анализа
        return result;
    }
}