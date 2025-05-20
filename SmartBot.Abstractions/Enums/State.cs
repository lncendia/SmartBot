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
    /// Ожидает ввода комментария от пользователя при отклонении отчёта.
    /// </summary>
    AwaitingRejectCommentInput,
    
    /// <summary>
    /// Ожидает ввода ответа на сообщение от пользователя.
    /// </summary>
    AwaitingAnswerInput,

    /// <summary>
    /// Ожидает ввода идентификатора рабочего чата для добавления.
    /// </summary>
    AwaitingWorkingChatIdForAdding,

    /// <summary>
    /// Ожидает ввода идентификатора администратора для добавления.
    /// </summary>
    AwaitingAdminIdForAdding,
    
    /// <summary>
    /// Ожидает ввода идентификатора теле-администратора для добавления.
    /// </summary>
    AwaitingTeleAdminIdForAdding,

    /// <summary>
    /// Ожидает ввода идентификатора администратора для удаления.
    /// </summary>
    AwaitingAdminIdForRemoval,
    
    /// <summary>
    /// Ожидает ввода идентификатора пользователя для удаления.
    /// </summary>
    AwaitingUserIdForBlock,
    
    /// <summary>
    /// Ожидает ввода идентификатора пользователя для установки рабочего чата.
    /// </summary>
    AwaitingUserIdForSetWorkingChat,
}