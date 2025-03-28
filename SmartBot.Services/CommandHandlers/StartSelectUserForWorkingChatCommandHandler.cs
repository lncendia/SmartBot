using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Models.WorkingChats;
using SmartBot.Services.Extensions;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для начала выбора пользователя для назначения рабочего чата.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class StartSelectUserForWorkingChatCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork)
    : IRequestHandler<StartSelectUserForWorkingChatCommand>
{
    /// <summary>
    /// Сообщение с инструкцией по выбору пользователя для назначения чата.
    /// </summary>
    private const string SelectUserInstructionMessage =
        "<b>👤 Выбор пользователя</b>\n\n" +
        "Выберите пользователя, которому нужно назначить рабочий чат.";
    
    /// <summary>
    /// Сообщение, которое отправляется, если рабочий чат не найден.
    /// </summary>
    private const string WorkingChatNotFoundMessage =
        "❌ Рабочий чат не найден.";

    /// <summary>
    /// Сообщение с подтверждением выбора чата.
    /// </summary>
    private const string ChatSelectedConfirmationMessage =
        "<b>✅ Чат выбран</b>\n\n" +
        "Теперь выберите пользователя для назначения этого рабочего чата.";

    /// <summary>
    /// Обрабатывает команду начала выбора пользователя для рабочего чата.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartSelectUserForWorkingChatCommand request, CancellationToken cancellationToken)
    {
        // Проверяем права администратора у инициатора команды
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;
        
        // Получаем рабочий чат из базы данных
        var workingChat = await unitOfWork.Query<WorkingChat>()
            .FirstOrDefaultAsync(c => c.Id == request.WorkingChatId, cancellationToken);

        // Если рабочий чат не найден
        if (workingChat == null)
        {
            // Уведомляем администратора об ошибке
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: WorkingChatNotFoundMessage,
                cancellationToken: cancellationToken
            );
            
            // Удаляем исходное сообщение с кнопками
            await request.TryDeleteMessageAsync(client, cancellationToken);
            return;
        }

        // Устанавливаем состояние ожидания выбора пользователя
        request.User!.State = State.AwaitingUserIdForSetWorkingChat;

        // Сохраняем выбранный ID рабочего чата
        request.User.SelectedWorkingChatId = request.WorkingChatId;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            // Обновляем сообщение с подтверждением выбора чата
            await client.EditMessageText(
                chatId: request.ChatId,
                messageId: request.MessageId,
                text: ChatSelectedConfirmationMessage,
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
                text: ChatSelectedConfirmationMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: CancellationToken.None
            );
        }

        // Отправляем сообщение с инструкцией по выбору пользователя
        await client.SendMessage(
            chatId: request.ChatId,
            text: SelectUserInstructionMessage,
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboard.SelectUserKeyboard,
            cancellationToken: CancellationToken.None
        );
    }
}