using MediatR;
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
/// Обработчик команды для удаления пользователя из числа проверяющих.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="logger">Логгер.</param>
public class RemoveExaminerCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    ILogger<RemoveExaminerCommandHandler> logger)
    : IRequestHandler<RemoveExaminerCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не является проверяющим.
    /// </summary>
    private const string NotExaminerMessage =
        "<b>❌ Ошибка:</b> Вы не являетесь проверяющим. Только проверяющие могут удалять других проверяющих.";

    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не найден.
    /// </summary>
    private const string UserNotFoundMessage =
        "<b>❌ Ошибка:</b> Пользователь с указанным ID не найден.";

    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не является проверяющим.
    /// </summary>
    private const string NotAnExaminerMessage =
        "<b>❌ Ошибка:</b> Пользователь не является проверяющим.";

    /// <summary>
    /// Сообщение об успешном удалении проверяющего.
    /// </summary>
    private const string ExaminerRemovedSuccessMessage =
        "<b>✅ Пользователь успешно удалён из числа проверяющих!</b>";

    /// <summary>
    /// Сообщение для удалённого проверяющего.
    /// </summary>
    private const string ExaminerRemovedNotificationMessage =
        "<b>ℹ️ Уведомление:</b>\n\n" +
        "Вы были удалены из числа проверяющих. Теперь вы не можете просматривать и комментировать отчёты.";

    /// <summary>
    /// Сообщение об ошибке, которое отправляется, если введённый ID пользователя имеет некорректный формат.
    /// </summary>
    private const string InvalidUserIdFormatMessage =
        "<b>❌ Ошибка:</b> Некорректный формат ID пользователя. Пожалуйста, введите числовой идентификатор.";
    
    /// <summary>
    /// Обрабатывает команду удаления пользователя из числа проверяющих.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(RemoveExaminerCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли пользователь проверяющим
        if (!request.User!.IsExaminer)
        {
            // Устанавливаем проверяющему состояние AwaitingReportInput
            request.User.State = State.AwaitingReportInput;

            // Сохраняем изменения в базе данных
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
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
        
        // Пытаемся преобразовать строку ExaminerId в число (long)
        if (!long.TryParse(request.ExaminerId, out var examinerId))
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

        // Получаем пользователя, которого нужно удалить из числа проверяющих
        var examinerToRemove = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(u => u.Id == examinerId, cancellationToken);

        // Если пользователь не найден
        if (examinerToRemove == null)
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

        // Если пользователь не является проверяющим
        if (!examinerToRemove.IsExaminer)
        {
            // Отправляем сообщение о том, что пользователь не является проверяющим
            await client.SendMessage(
                chatId: request.ChatId,
                text: NotAnExaminerMessage,
                parseMode: ParseMode.Html,
                replyMarkup: ExamKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Удаляем пользователя из числа проверяющих
        examinerToRemove.IsExaminer = false;
        examinerToRemove.State = State.AwaitingReportInput;

        // Устанавливаем состояние текущего пользователя на Idle
        request.User.State = State.Idle;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение об успешном удалении проверяющего текущему пользователю
        await client.SendMessage(
            chatId: request.ChatId,
            text: ExaminerRemovedSuccessMessage,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );

        // Отправляем сообщение удалённому проверяющему
        try
        {
            await client.SendMessage(
                chatId: examinerToRemove.Id,
                text: ExaminerRemovedNotificationMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException ex)
        {
            // Логируем ошибку, если не удалось отправить сообщение
            logger.LogWarning(ex, "Couldn't send a message to the deleted examiner with the ID {ExaminerId}.", examinerToRemove.Id);
        }
    }
}