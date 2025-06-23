using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Models.Users;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для изменения должности пользователя администратором.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="logger">Логгер для записи событий.</param>
public class EditUserPositionCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    ILogger<EditUserPositionCommandHandler> logger)
    : IRequestHandler<EditUserPositionCommand>
{
    /// <summary>
    /// Сообщение об ошибке валидации должности.
    /// Указывает на требования к длине должности.
    /// </summary>
    private const string PositionValidationErrorMessage =
        "<b>❌Ошибка:</b> Должность введена некорректно. " +
        "Пожалуйста, введите её заново. Длина должности не должна превышать 100 символов.";

    /// <summary>
    /// Сообщение об ошибке, если пользователь не найден.
    /// </summary>
    private const string UserNotFoundMessage =
        "<b>❌ Ошибка:</b> Пользователь с указанным ID не найден.";

    /// <summary>
    /// Сообщение об успешном изменении должности пользователя.
    /// </summary>
    private const string PositionUpdatedSuccessMessage =
        "<b>✅ Должность пользователя успешно изменена!</b>\n\n" +
        "Новая должность: <b>{0}</b>";

    /// <summary>
    /// Сообщение-уведомление для пользователя об изменении его должности.
    /// </summary>
    private const string PositionUpdatedNotificationMessage =
        "<b>ℹ️ Ваши данные были изменены администратором</b>\n\n" +
        "Ваша новая должность: <b>{0}</b>";

    /// <summary>
    /// Обрабатывает команду изменения должности пользователя.
    /// </summary>
    /// <param name="request">Запрос на изменение должности пользователя.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(EditUserPositionCommand request, CancellationToken cancellationToken)
    {
        // Получаем пользователя для изменения
        var userToEdit = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(u => u.Id == request.User!.SelectedUserId, cancellationToken);

        // Если пользователь не найден
        if (userToEdit == null)
        {
            await client.SendMessage(
                chatId: request.ChatId,
                text: UserNotFoundMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );
            return;
        }

        // Валидация введенной должности
        if (!IsPositionValid(request.Position))
        {
            await client.SendMessage(
                chatId: request.ChatId,
                text: PositionValidationErrorMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );
            return;
        }

        // Обновляем должность пользователя
        userToEdit.Position = request.Position;

        // Сохраняем изменения
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Уведомляем администратора
        await client.SendMessage(
            chatId: request.ChatId,
            text: string.Format(PositionUpdatedSuccessMessage, request.Position),
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboard.GoBackKeyboard,
            cancellationToken: cancellationToken
        );

        // Уведомляем пользователя об изменении
        try
        {
            await client.SendMessage(
                chatId: userToEdit.Id,
                text: string.Format(PositionUpdatedNotificationMessage, request.Position),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException ex)
        {
            logger.LogWarning(ex, "Failed to send position update notification to user {UserId}", userToEdit.Id);
        }
    }

    /// <summary>
    /// Валидирует введённую должность.
    /// </summary>
    /// <param name="position">Введённая должность.</param>
    /// <returns>True, если должность корректна, иначе false.</returns>
    private static bool IsPositionValid(string? position)
    {
        // Проверка, что должность не пустая
        if (string.IsNullOrWhiteSpace(position)) return false;

        // Проверка, что длина должности не превышает 100 символов
        return position.Length <= 100;
    }
}