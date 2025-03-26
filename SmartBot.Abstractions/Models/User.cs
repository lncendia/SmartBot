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
    public Role Role { get; set; } = Role.Employee;

    /// <summary>
    /// Идентификатор рабочего чата пользователя
    /// </summary>
    public long? WorkingChatId { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public bool IsAdmin => Role is Role.Admin or Role.TeleAdmin;
    
    /// <summary>
    /// 
    /// </summary>
    public bool IsEmployee => Role is Role.Employee or Role.TeleAdmin;

    /// <summary>
    /// Идентификатор проверяемого отчёта.
    /// </summary>
    public Guid? ReviewingReportId { get; set; }
    
    /// <summary>
    /// Идентификатор рабочего чата, который выбрал администратор для установки пользователю.
    /// </summary>
    public long? SelectedWorkingChatId { get; set; }

    /// <summary>
    /// Навигационное свойство
    /// </summary>
    public List<Report> Reports { get; init; } = [];
}