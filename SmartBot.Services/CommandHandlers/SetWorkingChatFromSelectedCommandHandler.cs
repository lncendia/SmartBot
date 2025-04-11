using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Models.Users;
using SmartBot.Abstractions.Models.WorkingChats;
using SmartBot.Services.Keyboards;
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
public class SetWorkingChatFromSelectedCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    ILogger<SetWorkingChatCommand> logger)
    : IRequestHandler<SetWorkingChatFromSelectedCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не найден.
    /// </summary>
    private const string UserNotFoundMessage =
        "<b>❌ Ошибка:</b> Пользователь с указанным ID не найден.";

    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не является сотрудником.
    /// </summary>
    private const string UserNotEmployeeMessage =
        "<b>❌ Ошибка:</b> Пользователь не является сотрудником.";

    /// <summary>
    /// Сообщение, которое отправляется, если чат не найден.
    /// </summary>
    private const string ChatNotFoundMessage =
        "<b>❌ Ошибка:</b> Указанный чат не найден.";

    /// <summary>
    /// Сообщение об успешной установке рабочего чата.
    /// </summary>
    private const string WorkingChatSetSuccessMessage =
        "<b>✅ Рабочий чат успешно установлен!</b>";

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
    public async Task Handle(SetWorkingChatFromSelectedCommand request, CancellationToken cancellationToken)
    {
        // Получаем пользователя, которому нужно установить рабочий чат
        var targetUser = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        
        // Если пользователь не найден
        if (targetUser == null)
        {
            // Отправляем сообщение о том, что пользователь не найден
            await client.SendMessage(
                chatId: request.ChatId,
                text: UserNotFoundMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Если пользователь не является сотрудником
        if (!targetUser.IsEmployee)
        {
            // Отправляем сообщение о том, что пользователь не является сотрудником
            await client.SendMessage(
                chatId: request.ChatId,
                text: UserNotEmployeeMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Получаем чат, который нужно установить как рабочий
        var targetChat = await unitOfWork.Query<WorkingChat>()
            .FirstOrDefaultAsync(c => c.Id == request.User!.SelectedWorkingChatId, cancellationToken);

        // Если чат не найден
        if (targetChat == null)
        {
            // Отправляем сообщение об ошибке
            await client.SendMessage(
                chatId: request.ChatId,
                text: ChatNotFoundMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Устанавливаем пользователю новый рабочий чат
        targetUser.WorkingChatId = targetChat.Id;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение об успешном выполнении команды
        await client.SendMessage(
            chatId: request.ChatId,
            text: WorkingChatSetSuccessMessage,
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboard.GoBackKeyboard,
            cancellationToken: CancellationToken.None
        );

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