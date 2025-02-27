using SmartBot.Abstractions.Enums;

namespace SmartBot.Abstractions.Models;

/// <summary>
/// Модель пользователя.
/// </summary>
public class User
{
    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public required long Id { get; init; }

    /// <summary>
    /// Полное имя пользователя.
    /// </summary>
    public string? FullName { get; set; }
    
    /// <summary>
    /// Должность пользователя.
    /// </summary>
    public string? Position { get; set; }

    /// <summary>
    /// Время регистрации пользователя.
    /// </summary>
    public DateTime RegistrationTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Состояние пользователя.
    /// </summary>
    public State State { get; set; } = State.AwaitingFullNameInput;

    /// <summary>
    /// Флаг, указывающий, является ли пользователь экспертом.
    /// </summary>
    public bool IsExaminer { get; set; }
    
    /// <summary>
    /// Идентификатор проверяемого отчёта.
    /// </summary>
    public Guid? ReviewingReportId { get; set; }
    
    /// <summary>
    /// Навигационное свойство
    /// </summary>
    public List<Report> Reports { get; init; } = [];
}