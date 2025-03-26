using MediatR;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Services.Extensions;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для начала процесса назначения администратора или теле-администратора.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class StartSelectUserForAssignAdminCommandHandler(ITelegramBotClient client, IUnitOfWork unitOfWork)
    : IRequestHandler<StartSelectUserForAssignAdminCommand>
{
    /// <summary>
    /// Сообщение о процессе добавления администратора
    /// </summary>
    private const string AddAdminInfoMessage =
        "<b>👑 Добавление администратора</b>\n\n" +
        "Вы можете назначить нового администратора системы.";

    /// <summary>
    /// Сообщение о процессе добавления теле-администратора
    /// </summary>
    private const string AddTeleAdminInfoMessage =
        "<b>👑 Добавление теле-администратора</b>\n\n" +
        "Вы можете назначить нового теле-администратора системы.";

    /// <summary>
    /// Сообщение с инструкцией по выбору администратора
    /// </summary>
    private const string SelectAdminMessage =
        "👇 Пожалуйста, выберите пользователя, " +
        "которому хотите предоставить права администратора.";

    /// <summary>
    /// Сообщение с инструкцией по выбору теле-администратора
    /// </summary>
    private const string SelectTeleAdminMessage =
        "👇 Пожалуйста, выберите пользователя, " +
        "которому хотите предоставить права теле-администратора.";

    /// <summary>
    /// Обрабатывает команду начала процесса назначения администратора.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartSelectUserForAssignAdminCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, имеет ли пользователь права администратора
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;

        // Устанавливаем соответствующее состояние в зависимости от типа назначаемого администратора
        request.User!.State = request.TeleAdmin
            ? State.AwaitingTeleAdminIdForAdding
            : State.AwaitingAdminIdForAdding;

        // Сохраняем изменения состояния пользователя в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Определяем текст сообщения в зависимости от типа администратора
        var infoMessage = request.TeleAdmin ? AddTeleAdminInfoMessage : AddAdminInfoMessage;
        var instructionMessage = request.TeleAdmin ? SelectTeleAdminMessage : SelectAdminMessage;

        try
        {
            // Редактируем существующее сообщение с информацией о процессе назначения
            await client.EditMessageText(
                chatId: request.ChatId,
                messageId: request.MessageId,
                text: infoMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException)
        {
            // Если редактирование сообщения не удалось, отправляем новое сообщение
            await client.SendMessage(
                chatId: request.ChatId,
                text: infoMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );
        }

        // Отправляем сообщение с инструкцией по выбору пользователя
        await client.SendMessage(
            chatId: request.ChatId,
            text: instructionMessage,
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboard.SelectUserKeyboard,
            cancellationToken: cancellationToken
        );
    }
}