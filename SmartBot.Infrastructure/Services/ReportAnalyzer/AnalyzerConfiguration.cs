namespace SmartBot.Infrastructure.Services.ReportAnalyzer;

/// <summary>
/// Конфигурация для анализатора отчётов.
/// </summary>
public class AnalyzerConfiguration
{
    /// <summary>
    /// Промпт для анализатора отчётов.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Модель для анализатора отчётов.
    /// </summary>
    public required string Model { get; init; }
}