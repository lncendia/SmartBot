using SmartBot.Abstractions.Interfaces.ReportAnalyzer.DTOs;

namespace SmartBot.Abstractions.Interfaces.ReportAnalyzer;

/// <summary>
/// Интерфейс для комплексного анализа рабочих отчетов с функциональностью:
/// 1. Базового анализа содержания
/// 2. Генерации утренней мотивации
/// 3. Формирования вечерней оценки
/// </summary>
/// <remarks>
/// Реализации интерфейса должны обеспечивать:
/// - Потокобезопасность
/// - Поддержку асинхронных операций
/// - Корректную обработку отмены операций
/// </remarks>
public interface IReportAnalyzer
{
    /// <summary>
    /// Выполняет базовый структурный анализ отчета
    /// </summary>
    /// <param name="report">Текст отчета для анализа. Не может быть null или пустым.</param>
    /// <param name="ct">Токен отмены для прерывания длительной операции.</param>
    /// <returns>Task с результатом анализа <see cref="ReportAnalysisResult"/></returns>
    /// <exception cref="ArgumentNullException">Если report равен null</exception>
    /// <exception cref="ArgumentException">Если report пуст или содержит только whitespace</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена через токен</exception>
    Task<ReportAnalysisResult> AnalyzeReportAsync(string report, CancellationToken ct = default);

    /// <summary>
    /// Генерирует мотивационный контент на основе утреннего отчета
    /// </summary>
    /// <param name="report">Текст утреннего отчета. Должен содержать планы на день.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Task с мотивационным результатом <see cref="MorningMotivationResult"/></returns>
    /// <remarks>
    /// Задачи анализа:
    /// - Анализировать запланированные задачи
    /// - Формировать позитивные рекомендации
    /// - Добавлять элемент мотивации
    /// - Включать легкий юмор
    /// </remarks>
    Task<MorningMotivationResult> GenerateMorningMotivationAsync(string report, CancellationToken ct = default);

    /// <summary>
    /// Формирует оценочный отзыв на основе вечернего отчета
    /// </summary>
    /// <param name="report">Текст вечернего отчета с выполненными задачами.</param>
    /// <param name="ct">Токен отмены операции.</param>
    /// <returns>Task с оценочным результатом <see cref="EveningPraiseResult"/></returns>
    /// <remarks>
    /// Задачи анализа:
    /// - Выделять выполненные задачи
    /// - Давать позитивную оценку работе
    /// - Подчеркивать достижения
    /// - Добавлять тематический юмор
    /// </remarks>
    Task<EveningPraiseResult> GenerateEveningPraiseAsync(string report, CancellationToken ct = default);
    
    /// <summary>
    /// Вычисляет балльную оценку эффективности работы на основе отчета.
    /// </summary>
    /// <param name="report">Текст отчета с перечнем выполненных задач.</param>
    /// <param name="ct">Токен для отмены асинхронной операции.</param>
    /// <returns>
    /// Числовая оценка эффективности в диапазоне от 0 до 10, где:
    /// 0 - минимальная продуктивность,
    /// 10 - максимальная продуктивность.
    /// </returns>
    /// <remarks>
    /// Алгоритм оценки учитывает:
    /// - Количество выполненных задач
    /// - Сложность выполненных задач
    /// - Соотношение выполненных и запланированных задач
    /// - Временные затраты
    /// </remarks>
    Task<double> GetScorePointsAsync(string report, CancellationToken ct = default);
}