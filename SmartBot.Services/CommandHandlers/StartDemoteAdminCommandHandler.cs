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
/// Обработчик команды для начала процесса удаления администратора.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class StartDemoteAdminCommandHandler(ITelegramBotClient client, IUnitOfWork unitOfWork)
    : IRequestHandler<StartDemoteAdminCommand>
{
    /// <summary>
    /// Сообщение с информацией о процессе удаления администратора
    /// </summary>
    private const string RemoveAdminInfoMessage =
        "<b>👨‍💻 Удаление администратора</b>\n\n" +
        "Вы можете снять администраторские права с пользователя.";

    /// <summary>
    /// Сообщение с инструкцией по выбору администратора для удаления
    /// </summary>
    private const string SelectAdminMessage =
        "👇 Пожалуйста, нажмите кнопку ниже и выберите пользователя, " +
        "которого хотите лишить прав администратора.";

    /// <summary>
    /// Обрабатывает команду начала процесса удаления администратора.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartDemoteAdminCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли пользователь администратором
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;

        // Устанавливаем состояние пользователя на AwaitingAdminIdForRemoval
        request.User!.State = State.AwaitingAdminIdForRemoval;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            // Изменяем информационное сообщение о процессе удаления
            await client.EditMessageText(
                chatId: request.ChatId,
                messageId: request.MessageId,
                text: RemoveAdminInfoMessage,
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
                text: RemoveAdminInfoMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: CancellationToken.None
            );
        }

        // Отправляем сообщение с инструкцией по выбору администратора
        await client.SendMessage(
            chatId: request.ChatId,
            text: SelectAdminMessage,
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboard.SelectUserKeyboard,
            cancellationToken: CancellationToken.None
        );
    }
}