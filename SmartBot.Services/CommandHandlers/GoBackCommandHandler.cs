using MediatR;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для возврата в состояние Idle.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="logger">Логгер.</param>
public class GoBackCommandHandler(ITelegramBotClient client, IUnitOfWork unitOfWork,
    ILogger<GoBackCommandHandler> logger)
    : IRequestHandler<GoBackCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не является проверяющим.
    /// </summary>
    private const string NotExaminerMessage =
        "<b>❌ Ошибка:</b> Вы не являетесь проверяющим. Только проверяющие могут использовать эту команду.";

    /// <summary>
    /// Обрабатывает команду возврата в состояние Idle.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(GoBackCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли пользователь проверяющим
        if (!request.User!.IsExaminer)
        {
            // Устанавливаем проверяющему состояние AwaitingReportInput
            request.User.State = State.AwaitingReportInput;

            // Сохраняем изменения в базе данных
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Отправляем сообщение о том, что пользователь не является проверяющим
            await client.SendMessage(
                chatId: request.ChatId,
                text: NotExaminerMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Устанавливаем состояние пользователя на Idle
        request.User.State = State.Idle;

        // Сбрасываем ID проверяемого отчёта
        request.User.ReviewingReportId = null;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Удаляем сообщение, которое инициировало команду
        try
        {
            await client.DeleteMessage(
                chatId: request.ChatId,
                messageId: request.MessageId,
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException ex)
        {
            // Логируем ошибку, если не удалось удалить сообщение
            logger.LogWarning(ex, "The message with the ID {messageId} could not be deleted.", request.MessageId);
        }
    }
}