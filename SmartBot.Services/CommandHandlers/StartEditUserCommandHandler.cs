using MediatR;
using SmartBot.Abstractions.Commands;
using SmartBot.Services.Extensions;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для начала процесса редактирования данных пользователя.
/// </summary>
/// <param name="client">Экземпляр клиента для взаимодействия с Telegram Bot API.</param>
public class StartEditUserCommandHandler(ITelegramBotClient client) : IRequestHandler<StartEditUserCommand>
{
    /// <summary>
    /// Сообщение с инструкцией по редактированию пользователя.
    /// </summary>
    private const string EditUserInfoMessage =
        "<b>✏️ Редактирование пользователя</b>\n\n" +
        "Вы можете изменить данные пользователя системы.";

    /// <summary>
    /// Обрабатывает команду начала процесса редактирования пользователя.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен для отмены операции.</param>
    public async Task Handle(StartEditUserCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, имеет ли пользователь права администратора
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;

        try
        {
            // Редактируем сообщение с инструкцией
            await client.EditMessageText(
                chatId: request.ChatId,
                messageId: request.MessageId,
                text: EditUserInfoMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.EditUserTypeKeyboard,
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException)
        {
            // Если редактирование сообщения невозможно, отправляем новое
            await client.SendMessage(
                chatId: request.ChatId,
                text: EditUserInfoMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.EditUserTypeKeyboard,
                cancellationToken: cancellationToken
            );
        }
    }
}