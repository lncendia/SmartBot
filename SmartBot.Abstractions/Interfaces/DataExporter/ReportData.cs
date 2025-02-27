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
    public string? MorningReport { get; set; }

    /// <summary>
    /// Вечерний отчет.
    /// </summary>
    public string? EveningReport { get; set; }

    /// <summary>
    /// Дата создания отчета.
    /// </summary>
    public required DateTime Date { get; init; }

    /// <summary>
    /// Комментарий, сгенерированный ботом.
    /// </summary>
    public string? Comment { get; set; }
}