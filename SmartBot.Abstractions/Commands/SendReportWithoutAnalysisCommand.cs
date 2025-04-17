﻿using SmartBot.Abstractions.Attributes;
using SmartBot.Abstractions.Commands.Abstractions;

namespace SmartBot.Abstractions.Commands;

/// <summary>
/// Команда для отправки отчёта без проверки анализатором.
/// </summary>
[AsyncCommand]
public class SendReportWithoutAnalysisCommand : CallbackQueryCommand
{
    /// <summary>
    /// Идентификатор сообщения с отчётом.
    /// </summary>
    public required int ReportMessageId { get; init; }
}