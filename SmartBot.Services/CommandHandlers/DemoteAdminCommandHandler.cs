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
/// Обработчик команды для удаления пользователя из числа администраторов.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="logger">Логгер.</param>
public class DemoteAdminCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    ILogger<DemoteAdminCommandHandler> logger)
    : IRequestHandler<DemoteAdminCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не найден.
    /// </summary>
    private const string UserNotFoundMessage =
        "<b>❌ Ошибка:</b> Пользователь с указанным ID не найден.";

    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не является администратором.
    /// </summary>
    private const string NotAnAdminMessage =
        "<b>❌ Ошибка:</b> Пользователь не является администратором.";

    /// <summary>
    /// Сообщение об успешном удалении администратора.
    /// </summary>
    private const string AdminRemovedSuccessMessage =
        "<b>✅ Пользователь успешно удалён из числа администраторов!</b>";

    /// <summary>
    /// Сообщение для удалённого администратора.
    /// </summary>
    private const string AdminRemovedNotificationMessage =
        "<b>ℹ️ Уведомление:</b>\n\n" +
        "Вы были удалены из числа администраторов. Теперь вы не можете просматривать и комментировать отчёты.";
    
    /// <summary>
    /// Обрабатывает команду удаления пользователя из числа администраторов.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(DemoteAdminCommand request, CancellationToken cancellationToken)
    {
        // Получаем пользователя, которого нужно удалить из числа администраторов
        var demotedAdmin = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(u => u.Id == request.AdminId, cancellationToken);

        // Если пользователь не найден
        if (demotedAdmin == null)
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

        // Если пользователь не является администратором
        if (!demotedAdmin.IsAdmin)
        {
            // Отправляем сообщение о том, что пользователь не является администратором
            await client.SendMessage(
                chatId: request.ChatId,
                text: NotAnAdminMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Удаляем пользователя из числа администраторов
        demotedAdmin.Role = Role.Employee;
        demotedAdmin.State = State.AwaitingReportInput;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение об успешном удалении администратора текущему пользователю
        await client.SendMessage(
            chatId: request.ChatId,
            text: AdminRemovedSuccessMessage,
            replyMarkup: AdminKeyboard.GoBackKeyboard,
            parseMode: ParseMode.Html,
            cancellationToken: CancellationToken.None
        );

        // Отправляем сообщение удалённому администратору
        try
        {
            await client.SendMessage(
                chatId: demotedAdmin.Id,
                text: AdminRemovedNotificationMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }
        catch (ApiRequestException ex)
        {
            // Логируем ошибку, если не удалось отправить сообщение
            logger.LogWarning(ex, "Couldn't send a message to the demoted administrator with the ID {AdminId}.", demotedAdmin.Id);
        }
    }
}