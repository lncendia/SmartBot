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
/// Обработчик команды для начала добавления нового чата.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class StartAddWorkingChatCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork)
    : IRequestHandler<StartAddWorkingChatCommand>
{
    /// <summary>
    /// Сообщение с информацией о возможностях администратора
    /// </summary>
    private const string AdminCapabilitiesMessage =
        "<b>🛠️ Добавление нового рабочего чата</b>\n\n" +
        "Как администратор, вы можете добавлять рабочие чаты в систему.\n" +
        "Добавленные чаты получают возможность отслеживания активности участников.\n";

    /// <summary>
    /// Сообщение с инструкцией по выбору чата для добавления
    /// </summary>
    private const string ChatSelectionMessage =
        "➕ Пожалуйста, нажмите кнопку ниже и выберите чат, который хотите добавить.";

    /// <summary>
    /// Обрабатывает команду начала добавления нового чата.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartAddWorkingChatCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли пользователь администратором
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;

        // Устанавливаем состояние пользователя на "Ожидание данных чата для добавления"
        request.User!.State = State.AwaitingWorkingChatIdForAdding;

        // Сохраняем изменения состояния пользователя в базу данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            // Изменяем информационное сообщение о возможностях администратора
            await client.EditMessageText(
                chatId: request.ChatId,
                messageId: request.MessageId,
                text: AdminCapabilitiesMessage,
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
                text: AdminCapabilitiesMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: CancellationToken.None
            );
        }

        // Отправляем сообщение с инструкцией по выбору чата и кнопкой выбора
        await client.SendMessage(
            chatId: request.ChatId,
            text: ChatSelectionMessage,
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboard.SelectChatKeyboard,
            cancellationToken: CancellationToken.None
        );
    }
}