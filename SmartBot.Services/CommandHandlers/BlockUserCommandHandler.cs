using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Models.Users;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для удаления пользователя.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="logger">Логгер.</param>
public class BlockUserCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    ILogger<BlockUserCommandHandler> logger)
    : IRequestHandler<BlockUserCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не найден.
    /// </summary>
    private const string UserNotFoundMessage =
        "<b>❌ Ошибка:</b> Пользователь с указанным ID не найден.";

    /// <summary>
    /// Сообщение об успешном удалении пользователя.
    /// </summary>
    private const string UserRemovedSuccessMessage =
        "<b>✅ Пользователь успешно заблокирован!</b>";

    /// <summary>
    /// Сообщение для удалённого пользователя.
    /// </summary>
    private const string UserRemovedNotificationMessage =
        "<b>ℹ️ Уведомление:</b>\n\n" +
        "Ваш аккаунт был заблокирован. Вы больше не можете использовать бота.";
    
    /// <summary>
    /// Обрабатывает команду удаления пользователя.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(BlockUserCommand request, CancellationToken cancellationToken)
    {
        // Получаем пользователя, которого нужно удалить
        var userToRemove = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        // Если пользователь не найден
        if (userToRemove == null)
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

        // Устанавливаем состояние пользователя на Blocked
        userToRemove.Role = Role.Blocked;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение об успешном удалении пользователя текущему пользователю
        await client.SendMessage(
            chatId: request.ChatId,
            text: UserRemovedSuccessMessage,
            replyMarkup: AdminKeyboard.GoBackKeyboard,
            parseMode: ParseMode.Html,
            cancellationToken: CancellationToken.None
        );

        // Отправляем сообщение удалённому пользователю
        try
        {
            await client.SendMessage(
                chatId: userToRemove.Id,
                text: UserRemovedNotificationMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }
        catch (ApiRequestException ex)
        {
            // Логируем ошибку, если не удалось отправить сообщение
            logger.LogWarning(ex, "Couldn't send message to deleted user with ID {UserId}.", userToRemove.Id);
        }
    }
}