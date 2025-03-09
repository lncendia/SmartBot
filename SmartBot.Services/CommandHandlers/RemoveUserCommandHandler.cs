﻿using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Models;
using SmartBot.Services.Keyboards.ExaminerKeyboard;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для удаления пользователя.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="logger">Логгер.</param>
public class RemoveUserCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    ILogger<RemoveUserCommandHandler> logger)
    : IRequestHandler<RemoveUserCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не является проверяющим.
    /// </summary>
    private const string NotExaminerMessage =
        "<b>❌ Ошибка:</b> Вы не являетесь проверяющим. Только проверяющие могут удалять пользователей.";

    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не найден.
    /// </summary>
    private const string UserNotFoundMessage =
        "<b>❌ Ошибка:</b> Пользователь с указанным ID не найден.";

    /// <summary>
    /// Сообщение об успешном удалении пользователя.
    /// </summary>
    private const string UserRemovedSuccessMessage =
        "<b>✅ Пользователь успешно удалён!</b>";

    /// <summary>
    /// Сообщение для удалённого пользователя.
    /// </summary>
    private const string UserRemovedNotificationMessage =
        "<b>ℹ️ Уведомление:</b>\n\n" +
        "Ваш аккаунт был заблокирован. Вы больше не можете использовать бота.";

    /// <summary>
    /// Сообщение об ошибке, которое отправляется, если введённый ID пользователя имеет некорректный формат.
    /// </summary>
    private const string InvalidUserIdFormatMessage =
        "<b>❌ Ошибка:</b> Некорректный формат ID пользователя. Пожалуйста, введите числовой идентификатор.";
    
    /// <summary>
    /// Обрабатывает команду удаления пользователя.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(RemoveUserCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли пользователь проверяющим
        if (!request.User!.IsExaminer)
        {
           // Отправляем сообщение о том, что пользователь не является проверяющим
            await client.SendMessage(
                chatId: request.ChatId,
                text: NotExaminerMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }
        
        // Пытаемся преобразовать строку UserId в число (long)
        if (!long.TryParse(request.UserId, out var userId))
        {
            // Если преобразование не удалось, отправляем сообщение об ошибке
            await client.SendMessage(
                chatId: request.ChatId,
                text: InvalidUserIdFormatMessage,
                parseMode: ParseMode.Html,
                replyMarkup: ExamKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Получаем пользователя, которого нужно удалить
        var userToRemove = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        // Если пользователь не найден
        if (userToRemove == null)
        {
            // Отправляем сообщение о том, что пользователь не найден
            await client.SendMessage(
                chatId: request.ChatId,
                text: UserNotFoundMessage,
                parseMode: ParseMode.Html,
                replyMarkup: ExamKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Устанавливаем состояние пользователя на Blocked
        userToRemove.State = State.Blocked;

        // Удаляем пользователя из числа проверяющих
        userToRemove.IsExaminer = false;
        
        // Устанавливаем состояние текущего пользователя на Idle
        request.User.State = State.Idle;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение об успешном удалении пользователя текущему пользователю
        await client.SendMessage(
            chatId: request.ChatId,
            text: UserRemovedSuccessMessage,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );

        // Отправляем сообщение удалённому пользователю
        try
        {
            await client.SendMessage(
                chatId: userToRemove.Id,
                text: UserRemovedNotificationMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException ex)
        {
            // Логируем ошибку, если не удалось отправить сообщение
            logger.LogWarning(ex, "Couldn't send message to deleted user with ID {UserId}.", userToRemove.Id);
        }
    }
}