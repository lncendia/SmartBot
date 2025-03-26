using MediatR;
using SmartBot.Abstractions.Commands;
using SmartBot.Services.Extensions;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для начала процесса добавления нового администратора.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
public class StartAssignAdminCommandHandler(ITelegramBotClient client) : IRequestHandler<StartAssignAdminCommand>
{
    /// <summary>
    /// Сообщение с информацией о процессе добавления администратора
    /// </summary>
    private const string AddAdminInfoMessage =
        "<b>👑 Добавление администратора</b>\n\n" +
        "Вы можете назначить нового администратора системы.";

    /// <summary>
    /// Обрабатывает команду начала процесса добавления нового администратора.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartAssignAdminCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли пользователь администратором
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;

        try
        {
            // Изменяем информационное сообщение о процессе добавления
            await client.EditMessageText(
                chatId: request.ChatId,
                messageId: request.MessageId,
                text: AddAdminInfoMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.SelectAdminTypeKeyboard,
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException)
        {
            // Если редактирование не удалось, отправляем новое сообщение
            await client.SendMessage(
                chatId: request.ChatId,
                text: AddAdminInfoMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.SelectAdminTypeKeyboard,
                cancellationToken: cancellationToken
            );
        }
    }
}