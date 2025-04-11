using MediatR;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Services.Extensions;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для начала процесса блокировки пользователя.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class StartBlockUserCommandHandler(ITelegramBotClient client, IUnitOfWork unitOfWork)
    : IRequestHandler<StartBlockUserCommand>
{
    /// <summary>
    /// Сообщение с информацией о процессе блокировки пользователя
    /// </summary>
    private const string BlockUserInfoMessage =
        "<b>⛔ Блокировка пользователя</b>\n\n" +
        "Вы можете заблокировать пользователя в системе.";

    /// <summary>
    /// Сообщение с инструкцией по выбору пользователя для блокировки
    /// </summary>
    private const string SelectUserMessage =
        "👇 Пожалуйста, нажмите кнопку ниже и выберите пользователя, " +
        "которого хотите заблокировать.";

    /// <summary>
    /// Обрабатывает команду начала процесса блокировки пользователя.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartBlockUserCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли пользователь администратором
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;

        // Устанавливаем состояние пользователя на AwaitingUserIdForRemoval
        request.User!.State = State.AwaitingUserIdForBlock;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            // Изменяем информационное сообщение о процессе блокировки
            await client.EditMessageText(
                chatId: request.ChatId,
                messageId: request.MessageId,
                text: BlockUserInfoMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: CancellationToken.None
            );
        }
        catch (ApiRequestException)
        {
            // Если редактирование не удалось, отправляем новое сообщение
            await client.SendMessage(
                chatId: request.ChatId,
                text: BlockUserInfoMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: CancellationToken.None
            );
        }
        
        // Отправляем сообщение с инструкцией по выбору пользователя
        await client.SendMessage(
            chatId: request.ChatId,
            text: SelectUserMessage,
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboard.SelectUserKeyboard,
            cancellationToken: CancellationToken.None
        );
    }
}