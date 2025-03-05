namespace SmartBot.Abstractions.Interfaces.DataExporter;

/// <summary>
/// Класс, представляющий данные отчета
/// </summary>
public class ReportData
{
    /// <summary>
    /// Идентификатор отчёта
    /// </summary>
    public Guid? Id { get; init; }
    
    /// <summary>
    /// Имя сотрудника
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Должность сотрудника
    /// </summary>
    public string? Position { get; init; }

    /// <summary>
    /// Утренний отчет.
    /// </summary>
    public string? MorningReport { get; init; }
    
    /// <summary>
    /// Время, на которое утренний отчёт был просрочен.
    /// Если отчёт сдан вовремя, значение равно null.
    /// </summary>
    public TimeSpan? MorningReportOverdue { get; init; }

    /// <summary>
    /// Вечерний отчет.
    /// </summary>
    public string? EveningReport { get; init; }
    
    /// <summary>
    /// Время, на которое вечерний отчёт был просрочен.
    /// Если отчёт сдан вовремя, значение равно null.
    /// </summary>
    public TimeSpan? EveningReportOverdue { get; init; }

    /// <summary>
    /// Дата создания отчета.
    /// </summary>
    public required DateTime Date { get; init; }

    /// <summary>
    /// Комментарий, сгенерированный ботом.
    /// </summary>
    public string? Comment { get; init; }
}