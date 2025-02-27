namespace SmartBot.Abstractions.Interfaces.DataExporter;

/// <summary>
/// Интерфейс для экспорта данных
/// </summary>
public interface IDataExporter
{
    /// <summary>
    /// Асинхронно экспортирует отчеты
    /// </summary>
    /// <param name="reports">Список отчетов для экспорта</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Задача, представляющая асинхронную операцию экспорта</returns>
    Task ExportReportsAsync(IReadOnlyList<ReportData> reports, CancellationToken cancellationToken = default);
}