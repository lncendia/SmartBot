namespace SmartBot.Abstractions.Interfaces.ReportAnalyzer;

/// <summary>
/// Интерфейс для анализа отчетов
/// </summary>
public interface IReportAnalyzer
{
    /// <summary>
    /// Анализирует отчет
    /// </summary>  
    /// <param name="report">Отчет</param>
    /// <param name="cancelationToken">Токен отмены операции</param>
    /// <returns>Результат анализа отчета</returns>
    Task<ReportAnalyzeResult> AnalyzeAsync(string report, CancellationToken cancelationToken = default);
}