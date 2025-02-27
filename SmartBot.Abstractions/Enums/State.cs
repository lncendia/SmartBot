namespace SmartBot.Abstractions.Enums;

/// <summary>
/// Состояния бота для управления диалогами и ожиданием ввода пользователя.
/// </summary>
public enum State
{
    /// <summary>
    /// Основное состояние бота (обычное состояние, когда бот не ожидает ввода).
    /// </summary>
    Idle = 1,

    /// <summary>
    /// Ожидает ввода ФИО от пользователя.
    /// </summary>
    AwaitingFullNameInput,

    /// <summary>
    /// Ожидает ввода должности от пользователя.
    /// </summary>
    AwaitingPositionInput,

    /// <summary>
    /// Ожидает ввода отчёта от пользователя.
    /// </summary>
    AwaitingReportInput,

    /// <summary>
    /// Ожидает ввода комментария от пользователя.
    /// </summary>
    AwaitingCommentInput,

    /// <summary>
    /// Ожидает ввода идентификатора администратора для добавления.
    /// </summary>
    AwaitingExaminerIdForAdding,

    /// <summary>
    /// Ожидает ввода идентификатора администратора для удаления.
    /// </summary>
    AwaitingExaminerIdForRemoval,
    
    /// <summary>
    /// Ожидает ввода идентификатора пользователя для удаления.
    /// </summary>
    AwaitingUserIdForRemoval
}