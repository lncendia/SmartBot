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
/// Обработчик команды для добавления нового проверяющего.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="logger">Логгер.</param>
public class AddExaminerCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    ILogger<AddExaminerCommandHandler> logger)
    : IRequestHandler<AddExaminerCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не является проверяющим.
    /// </summary>
    private const string NotExaminerMessage =
        "<b>❌ Ошибка:</b> Вы не являетесь проверяющим. Только проверяющие могут добавлять новых проверяющих.";

    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не найден.
    /// </summary>
    private const string UserNotFoundMessage =
        "<b>❌ Ошибка:</b> Пользователь с указанным ID не найден.";

    /// <summary>
    /// Сообщение, которое отправляется, если пользователь уже является проверяющим.
    /// </summary>
    private const string AlreadyExaminerMessage =
        "<b>❌ Ошибка:</b> Пользователь уже является проверяющим.";

    /// <summary>
    /// Сообщение об успешном добавлении проверяющего.
    /// </summary>
    private const string ExaminerAddedSuccessMessage =
        "<b>✅ Пользователь успешно назначен проверяющим!</b>";

    /// <summary>
    /// Сообщение для нового проверяющего.
    /// </summary>
    private const string NewExaminerMessage =
        "<b>🎉 Поздравляем!</b>\n\n" +
        "Вы были назначены проверяющим. Теперь вы можете просматривать и комментировать отчёты.";

    /// <summary>
    /// Сообщение об ошибке, которое отправляется, если введённый ID пользователя имеет некорректный формат.
    /// </summary>
    private const string InvalidUserIdFormatMessage =
        "<b>❌ Ошибка:</b> Некорректный формат ID пользователя. Пожалуйста, введите числовой идентификатор.";

    /// <summary>
    /// Обрабатывает команду добавления нового проверяющего.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(AddExaminerCommand request, CancellationToken cancellationToken)
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

        // Получаем пользователя, которого нужно назначить проверяющим
        var newExaminer = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(u => u.Id == examinerId, cancellationToken);

        // Если пользователь не найден
        if (newExaminer == null)
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

        // Если пользователь уже является проверяющим
        if (newExaminer.IsExaminer)
        {
            // Отправляем сообщение о том, что пользователь уже является проверяющим
            await client.SendMessage(
                chatId: request.ChatId,
                text: AlreadyExaminerMessage,
                parseMode: ParseMode.Html,
                replyMarkup: ExamKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Назначаем пользователя проверяющим
        newExaminer.IsExaminer = true;
        newExaminer.State = State.Idle;

        // Устанавливаем состояние текущего пользователя на Idle
        request.User.State = State.Idle;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение об успешном добавлении проверяющего текущему пользователю
        await client.SendMessage(
            chatId: request.ChatId,
            text: ExaminerAddedSuccessMessage,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );

        // Отправляем сообщение новому проверяющему
        try
        {
            await client.SendMessage(
                chatId: newExaminer.Id,
                text: NewExaminerMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException ex)
        {
            // Логируем ошибку, если не удалось отправить сообщение
            logger.LogWarning(ex, "Couldn't send a message to a new examiner with an ID {ExaminerId}.", newExaminer.Id);
        }
    }
}