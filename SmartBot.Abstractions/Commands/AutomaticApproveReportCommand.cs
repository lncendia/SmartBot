using MediatR;
using SmartBot.Abstractions.Models.Reports;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для принятия отчета автоматически
/// </summary>
public class AutomaticApproveReportCommand : IRequest
{
    /// <summary>
    /// Уникальный идентификатор проверяемого отчёта
    /// </summary>
    /// <value>
    /// GUID, однозначно идентифицирующий отчёт в системе.
    /// Используется для поиска и привязки комментариев.
    /// </value>
    public required Report Report { get; init; }

    /// <summary>
    /// Флаг, указывающий тип проверяемого отчёта
    /// </summary>
    /// <value>
    /// true - вечерний отчёт
    /// false - утренний отчёт
    /// </value>
    public required bool EveningReport { get; init; }
}