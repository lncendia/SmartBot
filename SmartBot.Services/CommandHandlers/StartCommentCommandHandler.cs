using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Models.Reports;
using SmartBot.Abstractions.Models.Users;
using SmartBot.Services.Extensions;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для начала ввода комментария к отчёту.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="dateTimeProvider">Провайдер для работы с текущим временем.</param>
public class StartCommentCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<StartCommentCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если отчёт не найден.
    /// </summary>
    private const string ReportNotFoundMessage = "❌ Отчёт не найден.";

    /// <summary>
    /// Сообщение, которое отправляется администратору для ввода комментария.
    /// </summary>
    private const string AwaitingCommentMessage =
        "<b>📝 Введите комментарий к отчёту:</b>\n\n" +
        "Пожалуйста, укажите ваши замечания или рекомендации для улучшения отчёта.";

    /// <summary>
    /// Сообщение, которое отправляется, если отчёт уже выгружен и комментарий не может быть добавлен.
    /// </summary>
    private const string ReportAlreadyExportedMessage = "⚠️ Отчёт уже выгружен.";

    /// <summary>
    /// Обрабатывает команду начала ввода комментария к отчёту.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartCommentCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли пользователь администратором
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;

        // Ищем отчёт в базе данных
        var report = await unitOfWork.Query<Report>()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        // Если отчёт не найден
        if (report == null)
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

        // Если это не сегодняшний отчёт
        if (report.Date.Date != dateTimeProvider.Now.Date)
        {
            // Отправляем сообщение о том, что это не сегодняшний отчёт
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: ReportAlreadyExportedMessage,
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

        // Устанавливаем состояние пользователя на AwaitingCommentInput
        request.User.State = State.AwaitingCommentInput;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение с запросом на ввод комментария
        await client.SendMessage(
            chatId: request.ChatId,
            text: AwaitingCommentMessage,
            parseMode: ParseMode.Html,
            replyMarkup: DefaultKeyboard.CancelKeyboard,
            cancellationToken: CancellationToken.None
        );
    }
}