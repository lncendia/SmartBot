using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Models.Reports;
using SmartBot.Abstractions.Models.Users;
using SmartBot.Services.Extensions;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для начала отклонения отчёта.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class StartRejectReportCommandHandler(ITelegramBotClient client, IUnitOfWork unitOfWork)
    : IRequestHandler<StartRejectReportCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если отчёт не найден.
    /// </summary>
    private const string ReportNotFoundMessage = "❌ Отчёт не найден.";

    /// <summary>
    /// Сообщение с инструкцией для администратора при отклонении отчёта.
    /// Запрашивает указание конкретных замечаний и причин отклонения.
    /// </summary>
    private const string RejectionFeedbackMessage =
        "<b>✏️ Укажите причину отклонения отчёта:</b>\n\n" +
        "Опишите, что именно нужно исправить в отчёте.\n" +
        "Это сообщение будет отправлено пользователю.";

    /// <summary>
    /// Сообщение, которое отправляется, если отчёт уже подтвержден и не может быть отклонен.
    /// </summary>
    private const string ReportAlreadyApprovedMessage = "⚠️ Отчёт уже был подтвержден.";

    /// <summary>
    /// Обрабатывает команду начала ввода комментария к отчёту.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartRejectReportCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли пользователь администратором
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;

        // Ищем отчёт в базе данных
        var report = await unitOfWork.Query<Report>()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        // Если отчёт не найден
        if (report == null || (request.EveningReport && report.EveningReport == null))
        {
            // Отправляем сообщение о том, что отчёт не найден
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: ReportNotFoundMessage,
                cancellationToken: cancellationToken
            );

            // Удаляем сообщение с командой
            await request.TryDeleteMessageAsync(client, cancellationToken);

            // Завершаем выполнение метода
            return;
        }

        // Если отчёт уже был принят
        if (report.GetReport(request.EveningReport)!.IsApproved)
        {
            // Отправляем сообщение о том, что это не сегодняшний отчёт
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: ReportAlreadyApprovedMessage,
                cancellationToken: cancellationToken
            );

            // Удаляем сообщение с командой
            await request.TryDeleteMessageAsync(client, cancellationToken);

            // Завершаем выполнение метода
            return;
        }

        // Устанавливаем у пользователя свойство ReviewingReport
        request.User!.ReviewingReport = new ReviewingReport
        {
            // Идентификатор отчёта
            ReportId = request.ReportId,

            // Флаг типа отчёта, утренний или вечерний
            EveningReport = request.EveningReport
        };

        // Устанавливаем состояние пользователя на AwaitingDenyCommentInput
        request.User.State = State.AwaitingRejectCommentInput;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение с запросом на ввод комментария
        await client.SendMessage(
            chatId: request.ChatId,
            text: RejectionFeedbackMessage,
            parseMode: ParseMode.Html,
            replyMarkup: DefaultKeyboard.CancelKeyboard,
            cancellationToken: CancellationToken.None
        );
    }
}