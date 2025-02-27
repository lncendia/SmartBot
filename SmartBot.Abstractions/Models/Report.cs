namespace SmartBot.Abstractions.Models;

/// <summary>
/// Класс, представляющий отчет.
/// </summary>
public class Report
{
    /// <summary>
    /// Уникальный идентификатор отчета.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Идентификатор пользователя, связанного с отчетом.
    /// </summary>
    public required long UserId { get; init; }
    
    /// <summary>
    /// Навигационное свойство
    /// </summary>
    public User? User { get; init; }
    
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
