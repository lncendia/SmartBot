using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Models.Users;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для добавления нового администратора.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="logger">Логгер.</param>
public class AssignAdminCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    ILogger<AssignAdminCommandHandler> logger)
    : IRequestHandler<AssignAdminCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не найден.
    /// </summary>
    private const string UserNotFoundMessage =
        "<b>❌ Ошибка:</b> Пользователь с указанным ID не найден.";

    /// <summary>
    /// Сообщение, которое отправляется, если пользователь уже является администратором.
    /// </summary>
    private const string AlreadyAdminMessage =
        "<b>❌ Ошибка:</b> Пользователь уже является администратором.";
    
    /// <summary>
    /// Сообщение, которое отправляется, если пользователь уже является теле-администратором.
    /// </summary>
    private const string AlreadyTeleAdminMessage =
        "<b>❌ Ошибка:</b> Пользователь уже является теле-администратором.";

    /// <summary>
    /// Сообщение об успешном добавлении администратора.
    /// </summary>
    private const string AdminAddedSuccessMessage =
        "<b>✅ Пользователь успешно назначен администратором!</b>";
    
    /// <summary>
    /// Сообщение об успешном добавлении теле-администратора.
    /// </summary>
    private const string TeleAdminAddedSuccessMessage =
        "<b>✅ Пользователь успешно назначен теле-администратором!</b>";

    /// <summary>
    /// Сообщение для нового администратора.
    /// </summary>
    private const string NewAdminMessage =
        "<b>🎉 Поздравляем!</b>\n\n" +
        "Вы были назначены администратором. Теперь вы можете просматривать и комментировать отчёты.";
    
    /// <summary>
    /// Сообщение для нового администратора.
    /// </summary>
    private const string NewTeleAdminMessage =
        "<b>🎉 Поздравляем!</b>\n\n" +
        "Вы были назначены теле-администратором. Теперь вы можете просматривать и комментировать отчёты.\n\n" +
        "<b>⚠️ Вам по-прежнему необходимо сдавать собственные отчёты.</b>";
    
    /// <summary>
    /// Обрабатывает команду добавления нового администратора.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(AssignAdminCommand request, CancellationToken cancellationToken)
    {
        // Получаем пользователя, которого нужно назначить администратором
        var newAdmin = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(u => u.Id == request.AdminId, cancellationToken);

        // Если пользователь не найден
        if (newAdmin == null)
        {
            // Отправляем сообщение о том, что пользователь не найден
            await client.SendMessage(
                chatId: request.ChatId,
                text: UserNotFoundMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }
        
        // Получаем роль, которую необходимо установить новому администратору
        var role = request.TeleAdmin
            ? Role.TeleAdmin 
            : Role.Admin;

        // Если пользователь уже является администратором
        if (newAdmin.Role == role)
        {
            // Формируем текст сообщения на основании роли администратора
            var message = request.TeleAdmin
                ? AlreadyTeleAdminMessage 
                : AlreadyAdminMessage;
            
            // Отправляем сообщение о том, что пользователь уже является администратором
            await client.SendMessage(
                chatId: request.ChatId,
                text: message,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Назначаем пользователя администратором
        newAdmin.Role = role;
        
        // Определяем новое состояние пользователя
        var newState = request.TeleAdmin
            ? State.AwaitingReportInput 
            : State.Idle;
        
        // Обновляем состояние пользователя
        newAdmin.State = newState;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Формируем текст сообщения на основании роли администратора
        var addedSuccessMessage = request.TeleAdmin
            ? TeleAdminAddedSuccessMessage 
            : AdminAddedSuccessMessage;
        
        // Отправляем сообщение об успешном добавлении администратора текущему пользователю
        await client.SendMessage(
            chatId: request.ChatId,
            text: addedSuccessMessage,
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboard.GoBackKeyboard,
            cancellationToken: CancellationToken.None
        );

        // Формируем текст сообщения на основании роли администратора
        var newAdminMessage = request.TeleAdmin
            ? NewTeleAdminMessage 
            : NewAdminMessage;
        
        // Отправляем сообщение новому администратору
        try
        {
            await client.SendMessage(
                chatId: newAdmin.Id,
                text: newAdminMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }
        catch (ApiRequestException ex)
        {
            // Логируем ошибку, если не удалось отправить сообщение
            logger.LogWarning(ex, "Couldn't send a message to the assigned administrator with an ID {AdminId}.", newAdmin.Id);
        }
    }
}