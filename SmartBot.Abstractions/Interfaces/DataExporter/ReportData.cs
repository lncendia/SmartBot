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
    public ReportElement? MorningReport { get; init; }
    
    /// <summary>
    /// Вечерний отчет.
    /// </summary>
    public ReportElement? EveningReport { get; init; }
    
    /// <summary>
    /// Дата создания отчета.
    /// </summary>
    public required DateTime Date { get; init; }

    /// <summary>
    /// Комментарий, сгенерированный ботом.
    /// </summary>
    public string? Comment { get; init; }
}

/// <summary>
/// Класс, представляющий отчёт пользователя (утренний или вечерний).
/// </summary>
public class ReportElement
{
    /// <summary>
    /// Данные отчёта (например, текст или структурированные данные).
    /// </summary>
    public required string Data { get; init; }

    /// <summary>
    /// Время, на которое отчёт был просрочен (если отчёт сдан с опозданием).
    /// Если отчёт сдан вовремя, значение равно null.
    /// </summary>
    public TimeSpan? Overdue { get; init; }
    
    /// <summary>
    /// Время сдачи отчёта. 
    /// </summary>
    public required TimeSpan TimeOfDay { get; init; }

    /// <summary>
    /// Флаг, указывающий был ли отчёт одобрен администратором.
    /// </summary>
    /// <value>
    /// true - отчёт проверен и одобрен администратором,
    /// false - отчёт не проверен или отклонён.
    /// </value>
    public bool Approved { get; set; }
}