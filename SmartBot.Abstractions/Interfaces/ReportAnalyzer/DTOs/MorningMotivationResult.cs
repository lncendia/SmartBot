namespace SmartBot.Abstractions.Interfaces.ReportAnalyzer.DTOs;

/// <summary>
/// Результат утренней мотивационной аналитики
/// </summary>
public class MorningMotivationResult
{
    /// <summary>
    /// Рекомендации по планированию дня (2-3 предложения)
    /// </summary>
    public required string Recommendations { get; init; }
    
    /// <summary>
    /// Мотивирующий текст (2-3 предложения)
    /// </summary>
    public required string Motivation { get; init; }
    
    /// <summary>
    /// Юмористическая заметка (1-2 предложения)
    /// </summary>
    public required string Humor { get; init; }
}