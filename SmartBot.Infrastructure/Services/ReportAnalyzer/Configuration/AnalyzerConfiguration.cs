namespace SmartBot.Infrastructure.Services.ReportAnalyzer.Configuration;

/// <summary>
/// Конфигурация для работы анализатора отчетов, содержащая настройки
/// для различных типов анализа и генерации контента.
/// </summary>
/// <remarks>
/// Позволяет гибко настраивать параметры обработки отчетов,
/// включая промпты и модели для разных сценариев использования.
/// </remarks>
public class AnalyzerConfiguration
{
    /// <summary>
    /// Идентификатор модели ИИ для обработки запроса.
    /// </summary>
    /// <value>
    /// Название или идентификатор модели (например, "gpt-4"),
    /// которая будет использоваться для генерации ответа.
    /// </value>
    public required string Model { get; init; }
    
    /// <summary>
    /// Конфигурация для базового анализа структуры отчета.
    /// </summary>
    /// <value>
    /// Содержит промпт для разбора и классификации задач в отчете.
    /// </value>
    public required string ReportAnalysisPrompt { get; init; }

    /// <summary>
    /// Конфигурация для генерации утренней мотивации.
    /// </summary>
    /// <value>
    /// Содержит промпт для создания мотивационных сообщений
    /// на основе планов на день.
    /// </value>
    public required string MorningMotivationPrompt { get; init; }
    
    /// <summary>
    /// Конфигурация для генерации вечерней оценки работы.
    /// </summary>
    /// <value>
    /// Содержит промпт для формирования похвалы
    /// и признания достижений за день.
    /// </value>
    public required string EveningPraisePrompt { get; init; }
    
    /// <summary>
    /// Конфигурация для оценки эффективности работы.
    /// </summary>
    /// <value>
    /// Содержит промпт для расчета балльной оценки
    /// продуктивности на основе отчета.
    /// </value>
    public required string ScorePointsPrompt { get; init; }
}