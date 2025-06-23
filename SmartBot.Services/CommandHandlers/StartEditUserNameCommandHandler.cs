using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Models.Users;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для начала процесса изменения имени пользователя.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class StartEditUserNameCommandHandler(ITelegramBotClient client, IUnitOfWork unitOfWork)
    : IRequestHandler<StartEditUserNameCommand>
{
    /// <summary>
    /// Сообщение с инструкцией по вводу нового имени пользователя
    /// </summary>
    private const string EditUserNameInfoMessage =
        "📌 <b>Введите новое ФИО пользователя в именительном падеже</b>\n\n" +
        "<i>Пример:</i> <code>Иванов Иван Иванович</code>.";

    /// <summary>
    /// Сообщение об ошибке, если пользователь не найден
    /// </summary>
    private const string UserNotFoundMessage =
        "<b>❌ Ошибка:</b> Пользователь с указанным ID не найден.";

    /// <summary>
    /// Обрабатывает команду начала процесса изменения имени пользователя.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartEditUserNameCommand request, CancellationToken cancellationToken)
    {
        // Получаем пользователя для изменения
        var userToEdit = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        // Если пользователь не найден
        if (userToEdit == null)
        {
            // Уведомляем об ошибке
            await client.SendMessage(
                chatId: request.ChatId,
                text: UserNotFoundMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );
            return;
        }

        // Устанавливаем состояние ожидания выбора пользователя
        request.User!.State = State.AwaitingNameForEdit;

        // Сохраняем выбранный ID пользователя
        request.User.SelectedUserId = request.UserId;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем новое сообщение с инструкцией
        await client.SendMessage(
            chatId: request.ChatId,
            text: EditUserNameInfoMessage,
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboard.GoBackKeyboard,
            cancellationToken: cancellationToken
        );
    }
}