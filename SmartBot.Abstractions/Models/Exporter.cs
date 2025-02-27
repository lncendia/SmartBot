namespace SmartBot.Abstractions.Models;

/// <summary>   
/// Модель для экспорта отчётов.
/// </summary>
public class Exporter
{
    /// <summary>
    /// Уникальный идентификатор.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Идентификатор последнего экспортированного отчёта.
    /// </summary>
    public Guid? LastExportedReportId { get; set; }
    
    /// <summary>
    /// Дата последнего экспорта.
    /// </summary>
    public DateTime? LastExportingDate { get; set; }
}