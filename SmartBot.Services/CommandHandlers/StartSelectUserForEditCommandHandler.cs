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
/// Обработчик команды для начала процесса назначения администратора или теле-администратора.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class StartSelectUserForEditCommandHandler(ITelegramBotClient client, IUnitOfWork unitOfWork)
    : IRequestHandler<StartSelectUserForEditCommand>
{
    /// <summary>
    /// Информация о добавлении администратора
    /// </summary>
    private const string AddAdminInfoMessage =
        "<b>👑 Добавление администратора</b>\n\n" +
        "Вы можете назначить нового администратора системы.";

    /// <summary>
    /// Инструкция по выбору пользователя для назначения администратором
    /// </summary>
    private const string AddAdminSelectUserMessage =
        "👇 Пожалуйста, выберите пользователя, " +
        "которому хотите предоставить права администратора.";

    /// <summary>
    /// Информация о добавлении теле-администратора
    /// </summary>
    private const string AddTeleAdminInfoMessage =
        "<b>👑 Добавление теле-администратора</b>\n\n" +
        "Вы можете назначить нового теле-администратора системы.";

    /// <summary>
    /// Инструкция по выбору пользователя для назначения теле-администратором
    /// </summary>
    private const string AddTeleAdminSelectUserMessage =
        "👇 Пожалуйста, выберите пользователя, " +
        "которому хотите предоставить права теле-администратора.";

    /// <summary>
    /// Информация об удалении администратора
    /// </summary>
    private const string RemoveAdminInfoMessage =
        "<b>👨‍💻 Удаление администратора</b>\n\n" +
        "Вы можете снять администраторские права с пользователя.";

    /// <summary>
    /// Инструкция по выбору администратора для удаления
    /// </summary>
    private const string RemoveAdminSelectUserMessage =
        "👇 Пожалуйста, нажмите кнопку ниже и выберите пользователя, " +
        "которого хотите лишить прав администратора.";

    /// <summary>
    /// Информация о блокировке пользователя
    /// </summary>
    private const string BlockUserInfoMessage =
        "<b>⛔ Блокировка пользователя</b>\n\n" +
        "Вы можете заблокировать пользователя в системе.";

    /// <summary>
    /// Инструкция по выбору пользователя для блокировки
    /// </summary>
    private const string BlockUserSelectUserMessage =
        "👇 Пожалуйста, нажмите кнопку ниже и выберите пользователя, " +
        "которого хотите заблокировать.";

    /// <summary>
    /// Сообщение о начале процесса редактирования имени пользователя
    /// </summary>
    private const string EditNameInfo =
        "✏️ <b>Редактирование имени</b>\n\nВы можете изменить имя выбранного пользователя.";

    /// <summary>
    /// Инструкция по выбору пользователя для изменения имени
    /// </summary>
    private const string SelectUserForNameEdit =
        "👇 Пожалуйста, выберите пользователя, имя которого вы хотите изменить.";

    /// <summary>
    /// Сообщение о начале процесса редактирования должности пользователя
    /// </summary>
    private const string EditPositionInfo =
        "✏️ <b>Редактирование должности</b>\n\nВы можете изменить должность выбранного пользователя.";

    /// <summary>
    /// Инструкция по выбору пользователя для изменения должности
    /// </summary>
    private const string SelectUserForPositionEdit =
        "👇 Пожалуйста, выберите пользователя, должность которого вы хотите изменить.";

    /// <summary>
    /// Обрабатывает команду начала процесса назначения администратора.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartSelectUserForEditCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, имеет ли пользователь права администратора
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;

        // Переменные для сообщений
        string infoMessage, selectMessage;

        // Определяем, какую операцию запрашивает пользователь
        switch (request.Command)
        {
            // Назначение администратора
            case AdminKeyboard.AssignAdminPart:

                // Устанавливаем состояние пользователя: ожидается ID пользователя для назначения администратором
                request.User!.State = State.AwaitingAdminIdForAdding;

                // Устанавливаем сообщение с информацией о процессе
                infoMessage = AddAdminInfoMessage;

                // Устанавливаем сообщение с инструкцией по выбору пользователя
                selectMessage = AddAdminSelectUserMessage;
                break;

            // Назначение теле-администратора
            case AdminKeyboard.AssignTeleAdminPart:

                // Устанавливаем состояние пользователя: ожидается ID пользователя для назначения теле-администратором
                request.User!.State = State.AwaitingTeleAdminIdForAdding;

                // Устанавливаем сообщение с информацией о процессе
                infoMessage = AddTeleAdminInfoMessage;

                // Устанавливаем сообщение с инструкцией по выбору пользователя
                selectMessage = AddTeleAdminSelectUserMessage;
                break;

            // Удаление администратора
            case AdminKeyboard.DemoteAdminPart:

                // Устанавливаем состояние пользователя: ожидается ID администратора для понижения
                request.User!.State = State.AwaitingAdminIdForRemoval;

                // Устанавливаем сообщение с информацией о процессе
                infoMessage = RemoveAdminInfoMessage;

                // Устанавливаем сообщение с инструкцией по выбору пользователя
                selectMessage = RemoveAdminSelectUserMessage;
                break;

            // Редактирование имени пользователя
            case AdminKeyboard.EditUserNamePart:

                // Устанавливаем состояние пользователя: ожидается ID пользователя для редактирования имени
                request.User!.State = State.AwaitingUserIdForEditName;

                // Устанавливаем сообщение с информацией о процессе
                infoMessage = EditNameInfo;

                // Устанавливаем сообщение с инструкцией по выбору пользователя
                selectMessage = SelectUserForNameEdit;
                break;

            // Редактирование должности пользователя
            case AdminKeyboard.EditUserPositionPart:

                // Устанавливаем состояние пользователя: ожидается ID пользователя для редактирования должности
                request.User!.State = State.AwaitingUserIdForEditPosition;

                // Устанавливаем сообщение с информацией о процессе
                infoMessage = EditPositionInfo;

                // Устанавливаем сообщение с инструкцией по выбору пользователя
                selectMessage = SelectUserForPositionEdit;
                break;

            // Блокировка пользователя
            case AdminKeyboard.BlockUserPositionPart:

                // Устанавливаем состояние пользователя: ожидается ID пользователя для блокировки
                request.User!.State = State.AwaitingUserIdForBlocking;

                // Устанавливаем сообщение с информацией о процессе
                infoMessage = BlockUserInfoMessage;

                // Устанавливаем сообщение с инструкцией по выбору пользователя
                selectMessage = BlockUserSelectUserMessage;
                break;

            // Если команда не распознана
            default:

                // Завершаем выполнение метода без действий
                return;
        }

        // Сохраняем изменения состояния пользователя в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

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
            text: selectMessage,
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboard.SelectUserKeyboard,
            cancellationToken: cancellationToken
        );
    }
}