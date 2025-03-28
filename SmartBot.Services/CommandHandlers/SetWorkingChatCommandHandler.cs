using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Models.Users;
using SmartBot.Abstractions.Models.WorkingChats;
using SmartBot.Services.Extensions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для установки рабочего чата пользователю.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="logger">Логгер.</param>
public class SetWorkingChatCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    ILogger<SetWorkingChatCommand> logger)
    : IRequestHandler<SetWorkingChatCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не найден.
    /// </summary>
    private const string UserNotFoundMessage = "❌ Пользователь не найден.";

    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не является сотрудником.
    /// </summary>
    private const string UserNotEmployeeMessage = "❌ Пользователь не является сотрудником.";

    /// <summary>
    /// Сообщение, которое отправляется, если чат не найден.
    /// </summary>
    private const string ChatNotFoundMessage = "❌ Указанный чат не найден.";

    /// <summary>
    /// Сообщение об успешной установке рабочего чата.
    /// </summary>
    private const string WorkingChatSetSuccessMessage = "✅ Рабочий чат успешно установлен!";

    /// <summary>
    /// Сообщение для пользователя об изменении рабочего чата.
    /// </summary>
    private const string WorkingChatChangedNotificationMessageFormat =
        "<b>ℹ️ Уведомление:</b>\n\n" +
        "Вам был назначен новый рабочий чат «<i>{0}</i>» для обработки отчётов.";

    /// <summary>
    /// Обрабатывает команду установки рабочего чата пользователю.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(SetWorkingChatCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли текущий пользователь администратором
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;

        // Получаем пользователя, которому нужно установить рабочий чат
        var targetUser = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        // Если пользователь не найден
        if (targetUser == null)
        {
            // Отправляем сообщение об ошибке
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: UserNotFoundMessage,
                cancellationToken: cancellationToken
            );

            // Удаляем исходное сообщение с кнопками
            await request.TryDeleteMessageAsync(client, cancellationToken);

            // Завершаем выполнение метода
            return;
        }

        // Если пользователь не является сотрудником
        if (!targetUser.IsEmployee)
        {
            // Отправляем сообщение о том, что пользователь не является сотрудником
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: UserNotEmployeeMessage,
                cancellationToken: cancellationToken
            );

            // Удаляем сообщение с командой
            await request.TryDeleteMessageAsync(client, cancellationToken);

            // Завершаем выполнение метода
            return;
        }

        // Получаем чат, который нужно установить как рабочий
        var targetChat = await unitOfWork.Query<WorkingChat>()
            .FirstOrDefaultAsync(c => c.Id == request.WorkingChatId, cancellationToken);

        // Если чат не найден
        if (targetChat == null)
        {
            // Отправляем сообщение об ошибке
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: ChatNotFoundMessage,
                cancellationToken: cancellationToken
            );

            // Удаляем исходное сообщение с кнопками
            await request.TryDeleteMessageAsync(client, cancellationToken);

            // Завершаем выполнение метода
            return;
        }

        // Устанавливаем пользователю новый рабочий чат
        targetUser.WorkingChatId = targetChat.Id;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение об успешном выполнении команды
        await client.AnswerCallbackQuery(
            callbackQueryId: request.CallbackQueryId,
            text: WorkingChatSetSuccessMessage,
            cancellationToken: CancellationToken.None
        );

        // Удаляем исходное сообщение с кнопками
        await request.TryDeleteMessageAsync(client, CancellationToken.None);

        // Уведомляем пользователя об изменении рабочего чата
        try
        {
            await client.SendMessage(
                chatId: targetUser.Id,
                text: string.Format(WorkingChatChangedNotificationMessageFormat, targetChat.Name),
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }
        catch (ApiRequestException ex)
        {
            // Логируем ошибку отправки уведомления пользователю
            logger.LogWarning(ex, "Failed to send working chat notification to user {UserId}.", targetUser.Id);
        }
    }
}