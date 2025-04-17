using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using SmartBot.Abstractions.Interfaces.ReportAnalyzer;
using SmartBot.Abstractions.Interfaces.ReportAnalyzer.DTOs;

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
    /// Логгер.
    /// </summary>
    private readonly ILogger<ResilientAnalyzer> _logger;

    /// <summary>
    /// Конструктор класса.
    /// </summary>
    /// <param name="innerAnalyzer">Реализация интерфейса анализатора отчетов.</param>
    /// <param name="logger">Логгер для записи событий и ошибок.</param>
    public ResilientAnalyzer(IReportAnalyzer innerAnalyzer, ILogger<ResilientAnalyzer> logger)
    {
        // Устанавливаем базовый анализатор
        _analyzer = innerAnalyzer;

        // Устанавливаем логгер
        _logger = logger;

        // Настройка пайплайна отказоустойчивости
        _pipeline = new ResiliencePipelineBuilder()

            // Добавляем стратегию обработки таймаутов
            .AddTimeout(new TimeoutStrategyOptions
            {
                // Генератор таймаута: 2 минуты
                TimeoutGenerator = static _ => new ValueTask<TimeSpan>(TimeSpan.FromMinutes(2)),

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
                SamplingDuration = TimeSpan.FromMinutes(5),

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

    /// <summary>
    /// Базовый метод для выполнения операций через отказоустойчивый пайплайн
    /// </summary>
    /// <typeparam name="TResult">Тип возвращаемого результата</typeparam>
    /// <param name="operation">Асинхронная операция для выполнения</param>
    /// <param name="operationName">Наименование операции для логирования</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат выполнения операции</returns>
    private async Task<TResult> ExecuteWithResilienceAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        string operationName,
        CancellationToken ct = default)
    {
        // Выполняем операцию через политику пайплайна
        var result = await _pipeline.ExecuteAsync(async token =>
            {
                try
                {
                    // Выполняем целевую операцию
                    return await operation(token);
                }
                catch (Exception ex)
                {
                    // Логируем ошибку с указанием имени операции
                    _logger.LogError(ex, "{Operation} завершилась с ошибкой", operationName);
                    throw;
                }
            },
            ct);

        // Возвращаем результат выполнения
        return result;
    }

    /// <inheritdoc/>
    /// <summary>
    /// Анализирует отчет с использованием отказоустойчивого пайплайна
    /// </summary>
    public Task<ReportAnalysisResult> AnalyzeReportAsync(string report, CancellationToken ct = default)
    {
        // Вызываем базовый метод с конкретной операцией
        return ExecuteWithResilienceAsync(
            async token => await _analyzer.AnalyzeReportAsync(report, token),
            "Анализ отчета",
            ct);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Генерирует утреннюю мотивацию на основе отчета
    /// </summary>
    public Task<MorningMotivationResult> GenerateMorningMotivationAsync(string report, CancellationToken ct = default)
    {
        // Вызываем базовый метод с конкретной операцией
        return ExecuteWithResilienceAsync(
            async token => await _analyzer.GenerateMorningMotivationAsync(report, token),
            "Генерация утренней мотивации",
            ct);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Формирует вечернюю оценку выполненной работы
    /// </summary>
    public Task<EveningPraiseResult> GenerateEveningPraiseAsync(string report, CancellationToken ct = default)
    {
        // Вызываем базовый метод с конкретной операцией
        return ExecuteWithResilienceAsync(
            async token => await _analyzer.GenerateEveningPraiseAsync(report, token),
            "Генерация вечерней оценки",
            ct);
    }

    /// <inheritdoc/>
    /// <summary>
    /// Вычисляет балльную оценку эффективности работы
    /// </summary>
    public Task<double> GetScorePointsAsync(string report, CancellationToken ct = default)
    {
        // Вызываем базовый метод с конкретной операцией
        return ExecuteWithResilienceAsync(
            async token => await _analyzer.GetScorePointsAsync(report, token),
            "Расчет баллов эффективности",
            ct);
    }
}