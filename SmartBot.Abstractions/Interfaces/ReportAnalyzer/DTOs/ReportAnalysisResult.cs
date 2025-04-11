namespace SmartBot.Abstractions.Interfaces.ReportAnalyzer.DTOs;

/// <summary>
/// Результат анализа отчета
/// </summary>
public class ReportAnalysisResult
{
    /// <summary>
    /// Оценка отчета
    /// </summary>
    public required double Score { get; init; }

    /// <summary>
    /// Рекомендации по улучшению отчета
    /// </summary>
    public required string Recommendations { get; init; }
}