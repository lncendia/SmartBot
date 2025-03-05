namespace SmartBot.Abstractions.Interfaces.ReportAnalyzer;

/// <summary>
/// Интерфейс для анализа отчетов
/// </summary>
public interface IReportAnalyzer
{
    /// <summary>
    /// Анализирует отчет
    /// </summary>  
    /// <param name="report">Отчет для анализа.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Результат анализа отчета.</returns>
    Task<ReportAnalyzeResult> AnalyzeAsync(string report, CancellationToken cancellationToken = default);
}