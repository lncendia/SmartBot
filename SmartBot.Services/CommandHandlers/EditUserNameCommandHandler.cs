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
/// Обработчик команды для валидации и установки нового имени пользователя администратором.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="logger">Логгер для записи событий.</param>
public class EditUserNameCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    ILogger<EditUserNameCommandHandler> logger)
    : IRequestHandler<EditUserNameCommand>
{
    /// <summary>
    /// Сообщение об ошибке валидации имени пользователя.
    /// Указывает на требования к формату и длине ФИО.
    /// </summary>
    private const string NameValidationErrorMessage =
        "<b>❌ Ошибка:</b> ФИО введено некорректно.\n\n" +
        "Требования:\n" +
        "• Формат: <b>Фамилия Имя Отчество</b>\n" +
        "• Максимальная длина: 150 символов\n\n" +
        "<i>Пример:</i> <code>Иванов Иван Иванович</code>";

    /// <summary>
    /// Сообщение об ошибке, если пользователь не найден.
    /// </summary>
    private const string UserNotFoundMessage =
        "<b>❌ Ошибка:</b> Пользователь с указанным ID не найден.";

    /// <summary>
    /// Сообщение об успешном изменении имени пользователя.
    /// </summary>
    private const string NameUpdatedSuccessMessage =
        "<b>✅ Имя пользователя успешно изменено!</b>";

    /// <summary>
    /// Сообщение-уведомление для пользователя об изменении его имени.
    /// </summary>
    private const string NameUpdatedNotificationMessage =
        "<b>ℹ️ Ваши данные были изменены администратором</b>\n\n" +
        "Ваше новое ФИО: <b>{0}</b>";

    /// <summary>
    /// Обрабатывает команду изменения имени пользователя администратором.
    /// </summary>
    /// <param name="request">Запрос на изменение имени пользователя.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(EditUserNameCommand request, CancellationToken cancellationToken)
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

        // Валидация введенного ФИО
        if (!IsFullNameValid(request.FullName))
        {
            await client.SendMessage(
                chatId: request.ChatId,
                text: NameValidationErrorMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );
            return;
        }

        // Обновляем имя пользователя
        userToEdit.FullName = request.FullName;

        // Сохраняем изменения
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Уведомляем администратора
        await client.SendMessage(
            chatId: request.ChatId,
            text: NameUpdatedSuccessMessage,
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboard.GoBackKeyboard,
            cancellationToken: cancellationToken
        );

        // Уведомляем пользователя об изменении
        try
        {
            await client.SendMessage(
                chatId: userToEdit.Id,
                text: string.Format(NameUpdatedNotificationMessage, request.FullName),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException ex)
        {
            logger.LogWarning(ex, "Failed to send name update notification to user {UserId}", userToEdit.Id);
        }
    }

    /// <summary>
    /// Валидирует введённое ФИО.
    /// </summary>
    /// <param name="fullName">Введённое ФИО.</param>
    /// <returns>True, если ФИО корректно, иначе false.</returns>
    private static bool IsFullNameValid(string? fullName)
    {
        // Проверка, что ФИО не пустое
        if (string.IsNullOrEmpty(fullName)) return false;

        // Проверка, что длина ФИО не превышает 150 символов
        if (fullName.Length > 150) return false;

        // Разделение ФИО на части (Фамилия, Имя, Отчество)
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Проверка, что ФИО состоит из трёх частей
        if (parts.Length != 3) return false;

        // Проверка каждой части ФИО
        foreach (var part in parts)
        {
            // Проверка, что часть не пустая
            if (string.IsNullOrWhiteSpace(part)) return false;

            // Проверка, что все символы в части являются буквами
            if (!part.All(char.IsLetter)) return false;

            // Проверка, что часть начинается с заглавной буквы
            if (!char.IsUpper(part[0])) return false;
        }

        // Если все проверки пройдены, возвращаем true
        return true;
    }
}