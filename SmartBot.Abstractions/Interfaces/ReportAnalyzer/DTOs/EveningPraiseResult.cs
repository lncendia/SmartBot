namespace SmartBot.Abstractions.Interfaces.ReportAnalyzer.DTOs;

/// <summary>
/// Результат вечерней оценочной аналитики
/// </summary>
public class EveningPraiseResult
{
    /// <summary>
    /// Признание достижений (2-3 предложения)
    /// </summary>
    public required string Achievements { get; init; }
    
    /// <summary>
    /// Похвала за проделанную работу (2-3 предложения)
    /// </summary>
    public required string Praise { get; init; }
    
    /// <summary>
    /// Юмористическая заметка (1-2 предложения)
    /// </summary>
    public required string Humor { get; init; }
}