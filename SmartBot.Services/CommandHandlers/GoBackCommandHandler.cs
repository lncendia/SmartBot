using MediatR;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Services.Extensions;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для возврата в состояние Idle.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class GoBackCommandHandler(ITelegramBotClient client, IUnitOfWork unitOfWork)
    : IRequestHandler<GoBackCommand>
{
    /// <summary>
    /// Приветственное сообщение административной панели
    /// </summary>
    private const string AdminPanelMessage =
        "<b>⚙️ Панель управления администратора</b>\n\n" +
        "Выберите действие из меню ниже:";
    
    /// <summary>
    /// Обрабатывает команду возврата в состояние Idle.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(GoBackCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли пользователь администратором
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;

        // Устанавливаем состояние пользователя на Idle
        request.User!.State = State.Idle;

        // Сбрасываем ID проверяемого отчёта
        request.User.ReviewingReportId = null;
        
        // Сбрасываем ID проверяемого чата
        request.User.SelectedWorkingChatId = null;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
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
            // Если редактирование не удалось, отправляем новое сообщение
            await client.SendMessage(
                chatId: request.ChatId,
                text: AdminPanelMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.MainKeyboard,
                cancellationToken: CancellationToken.None
            );
        }
        
        // Отправляем сообщение с ReplyKeyboardRemove, чтобы убрать клавиатуру
        var message = await client.SendMessage(
            chatId: request.ChatId,
            text: "<i>Убираю клавиатуру...</i>",
            parseMode: ParseMode.Html,
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: CancellationToken.None
        );

        // Удаляем служебное сообщение, которое убрало клавиатуру
        await client.DeleteMessage(
            chatId: request.ChatId,
            messageId: message.MessageId,
            cancellationToken: CancellationToken.None
        );
    }
}