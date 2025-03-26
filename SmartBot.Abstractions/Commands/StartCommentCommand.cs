using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для начала комментирования отчёта
/// </summary>
public class StartCommentCommand : AdminCallbackQuery
{
    /// <summary>
    /// Идентификатор отчёта
    /// </summary>
    public required Guid ReportId { get; init; }
}