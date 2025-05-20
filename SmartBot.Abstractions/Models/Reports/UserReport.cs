namespace SmartBot.Abstractions.Models.Reports;

/// <summary>
/// Класс, представляющий отчёт пользователя (утренний или вечерний).
/// </summary>
public class UserReport
{
    /// <summary>
    /// Идентификатор отчёта
    /// </summary>
    public int Id { get; init; }
    
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
    /// Дата сдачи отчёта. 
    /// </summary>
    public required DateTime Date { get; init; }

    /// <summary>
    /// Флаг, указывающий был ли отчёт одобрен администратором.
    /// </summary>
    /// <value>
    /// true - отчёт проверен и одобрен администратором,
    /// false - отчёт не проверен или отклонён.
    /// </value>
    public bool Approved { get; set; }

    /// <summary>
    /// Флаг, указывающий был ли отчёт автоматически одобрен системой.
    /// </summary>
    /// <value>
    /// true - отчёт автоматически одобрен системой без ручной проверки,
    /// false - отчёт требует ручной проверки администратором.
    /// </value>
    public bool ApprovedBySystem { get; set; }

    /// <summary>
    /// Флаг, указывающий был ли отчёт одобрен.
    /// </summary>
    public bool IsApproved => Approved || ApprovedBySystem;
}