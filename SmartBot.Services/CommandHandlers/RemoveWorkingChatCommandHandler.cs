using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Models;
using SmartBot.Services.Extensions;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для удаления чата.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class RemoveWorkingChatCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveWorkingChatCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если чат не найден.
    /// </summary>
    private const string ChatNotFoundMessage =
        "❌ Рабочий чат не найден.";

    /// <summary>
    /// Сообщение об успешном удалении чата.
    /// </summary>
    private const string ChatRemovedSuccessMessage =
        "✅ Чат успешно удалён!";

    /// <summary>
    /// Приветственное сообщение административной панели
    /// </summary>
    private const string AdminPanelMessage =
        "<b>⚙️ Панель управления администратора</b>\n\n" +
        "Выберите действие из меню ниже:";
    
    /// <summary>
    /// Обрабатывает команду удаления чата.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(RemoveWorkingChatCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли пользователь администратором
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;
        
        // Получаем чат из базы данных по его ID
        var chat = await unitOfWork.Query<WorkingChat>()
            .FirstOrDefaultAsync(c => c.Id == request.WorkingChatId, cancellationToken);

        // Если чат не найден
        if (chat == null)
        {
            // Отправляем сообщение о том, что чат не найден
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: ChatNotFoundMessage,
                cancellationToken: cancellationToken
            );
            
            // Удаляем исходное сообщение с кнопками
            await request.TryDeleteMessageAsync(client, cancellationToken);

            // Завершаем выполнение метода
            return;
        }

        // Удаляем чат из базы данных
        await unitOfWork.DeleteAsync(chat, cancellationToken);

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение об успешном удалении чата
        await client.AnswerCallbackQuery(
            callbackQueryId: request.CallbackQueryId,
            text: ChatRemovedSuccessMessage,
            cancellationToken: CancellationToken.None
        );
        
        try
        {
            // Изменяем сообщение с основным меню администратора
            await client.EditMessageText(
                chatId: request.ChatId,
                messageId: request.MessageId,
                text: AdminPanelMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.MainKeyboard,
                cancellationToken: CancellationToken.None
            );
        }
        catch (ApiRequestException)
        {
            // ignored
        }
    }
}