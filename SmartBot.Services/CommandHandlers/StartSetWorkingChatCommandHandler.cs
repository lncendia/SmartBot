using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Models.WorkingChats;
using SmartBot.Services.Extensions;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для начала процесса установки рабочего чата пользователю.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class StartSetWorkingChatCommandHandler(ITelegramBotClient client, IUnitOfWork unitOfWork)
    : IRequestHandler<StartSetWorkingChatCommand>
{
    /// <summary>
    /// Сообщение с информацией о процессе выбора рабочего чата
    /// </summary>
    private const string SelectWorkingChatInfoMessage =
        "<b>💼 Назначение рабочего чата</b>\n\n" +
        "Выберите чат, который будет назначен пользователю для работы с отчетами.";
    
    /// <summary>
    /// Сообщение, которое отправляется, если нет доступных чатов.
    /// </summary>
    private const string NoAvailableChatsMessage =
        "❌ Нет доступных чатов. Сначала добавьте рабочий чат.";
    
    /// <summary>
    /// Обрабатывает команду начала процесса установки рабочего чата.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartSetWorkingChatCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, имеет ли пользователь права администратора
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;
        
        // Получаем список всех рабочих чатов из базы данных
        var chats = await unitOfWork
            .Query<WorkingChat>()
            .Select(c => new ValueTuple<long, string>(c.Id, c.Name))
            .Take(99)
            .ToArrayAsync(cancellationToken);

        // Проверяем наличие доступных чатов
        if (chats.Length == 0)
        {
            // Уведомляем администратора об отсутствии чатов
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: NoAvailableChatsMessage,
                cancellationToken: cancellationToken
            );
            
            return;
        }
        
        try
        {
            // Редактируем существующее сообщение для выбора рабочего чата
            await client.EditMessageText(
                chatId: request.ChatId,
                messageId: request.MessageId,
                text: SelectWorkingChatInfoMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.SelectWorkingChatKeyboard(chats),
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException)
        {
            // Если редактирование не удалось, отправляем новое сообщение
            await client.SendMessage(
                chatId: request.ChatId,
                text: SelectWorkingChatInfoMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.SelectWorkingChatKeyboard(chats),
                cancellationToken: cancellationToken
            );
        }
    }
}