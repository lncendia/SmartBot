using System.Diagnostics.CodeAnalysis;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Models.Reports;

namespace SmartBot.Abstractions.Models.Users;

/// <summary>
/// Модель пользователя системы отчетности
/// </summary>
/// <remarks>
/// Основная сущность, содержащая информацию о пользователе и его состоянии в системе.
/// Хранит как персональные данные, так и рабочую контекстную информацию.
/// </remarks>
public class User
{
    /// <summary>
    /// Уникальный идентификатор пользователя в Telegram
    /// </summary>
    /// <value>
    /// Положительное число, соответствующее user_id в Telegram API
    /// </value>
    public required long Id { get; init; }

    /// <summary>
    /// Полное имя пользователя (ФИО)
    /// </summary>
    /// <value>
    /// Строка в формате "Фамилия Имя Отчество". Может быть null до завершения регистрации.
    /// </value>
    public string? FullName { get; set; }

    /// <summary>
    /// Должность/позиция пользователя в компании
    /// </summary>
    /// <value>
    /// Например: "Менеджер по продажам", "Разработчик Python". Может быть null.
    /// </value>
    public string? Position { get; set; }

    /// <summary>
    /// Дата и время регистрации пользователя в системе
    /// </summary>
    /// <value>
    /// UTC время. Заполняется автоматически при создании пользователя.
    /// </value>
    public DateTime RegistrationTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Текущее состояние пользователя в системе
    /// </summary>
    /// <value>
    /// Определяет доступные действия и ожидаемый ввод от пользователя.
    /// По умолчанию: Ожидание ввода ФИО (первый шаг регистрации).
    /// </value>
    public State State { get; set; } = State.AwaitingFullNameInput;

    /// <summary>
    /// Роль пользователя в системе
    /// </summary>
    /// <value>
    /// Определяет уровень доступа и функциональные возможности.
    /// По умолчанию: Сотрудник (базовые права).
    /// </value>
    public Role Role { get; set; } = Role.Employee;

    /// <summary>
    /// Идентификатор рабочего чата пользователя
    /// </summary>
    /// <value>
    /// ID чата, в котором пользователь отправляет отчеты.
    /// Null означает, что чат не назначен.
    /// </value>
    public long? WorkingChatId { get; set; }

    /// <summary>
    /// Флаг, указывающий наличие административных прав
    /// </summary>
    /// <value>
    /// true - пользователь имеет права Admin или TeleAdmin
    /// false - обычный сотрудник или заблокированный пользователь
    /// </value>
    public bool IsAdmin => Role is Role.Admin or Role.TeleAdmin;
    
    /// <summary>
    /// Флаг, указывающий статус сотрудника
    /// </summary>
    /// <value>
    /// true - пользователь является сотрудником (Employee или TeleAdmin)
    /// false - администратор или заблокированный пользователь
    /// </value>
    public bool IsEmployee => Role is Role.Employee or Role.TeleAdmin;

    /// <summary>
    /// Контекст проверки отчета (для администраторов)
    /// </summary>
    /// <value>
    /// Содержит информацию о проверяемом отчете.
    /// Null - нет активной проверки.
    /// </value>
    public ReviewingReport? ReviewingReport { get; set; }
    
    /// <summary>
    /// Временный идентификатор чата для назначения (администраторский функционал)
    /// </summary>
    /// <value>
    /// ID чата, выбранного администратором для назначения пользователю.
    /// Используется в процессе настройки рабочего пространства.
    /// </value>
    public long? SelectedWorkingChatId { get; set; }
    
    /// <summary>
    /// Контекст ответа на сообщение
    /// </summary>
    /// <value>
    /// Содержит информацию о сообщении, на которое пользователь отвечает.
    /// Null - нет активного ответа.
    /// </value>
    public AnswerFor? AnswerFor { get; set; }

    /// <summary>
    /// Коллекция отчетов пользователя
    /// </summary>
    /// <value>
    /// Навигационное свойство для доступа ко всем отчетам пользователя.
    /// Автоматически инициализируется пустым списком.
    /// </value>
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
    public List<Report> Reports { get; init; } = [];
}