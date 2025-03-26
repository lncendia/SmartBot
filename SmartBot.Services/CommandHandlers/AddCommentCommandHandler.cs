using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Models;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для добавления комментария к отчёту.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class AddCommentCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<AddCommentCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если отчёт не найден.
    /// </summary>
    private const string ReportNotFoundMessage =
        "<b>❌ Ошибка:</b> Отчёт не найден. Возможно, он был удалён или ещё не создан.";

    /// <summary>
    /// Сообщение, которое отправляется, если комментарий пустой.
    /// </summary>
    private const string EmptyCommentMessage =
        "<b>❌ Ошибка:</b> Комментарий не может быть пустым. Пожалуйста, введите текст комментария.";

    /// <summary>
    /// Сообщение, которое отправляется, если суммарная длина комментария превышает 1500 символов.
    /// </summary>
    private const string CommentTooLongMessage =
        "<b>❌ Ошибка:</b> Суммарная длина комментария превышает 1500 символов. Пожалуйста, сократите текст.";

    /// <summary>
    /// Сообщение об успешном добавлении комментария.
    /// </summary>
    private const string CommentAddedSuccessMessage =
        "<b>✅ Комментарий успешно добавлен!</b>\n\n" +
        "Теперь вы можете продолжить работу с другими отчётами.";

    /// <summary>
    /// Сообщение, которое отправляется, если отчёт уже выгружен и комментарий не может быть добавлен.
    /// </summary>
    private const string ReportAlreadyExportedMessage =
        "<b>⚠️ Информация:</b> Отчёт уже выгружен.\n\n" +
        "Комментарий не может быть добавлен к выгруженному отчёту.";
    
    /// <summary>
    /// Обрабатывает команду добавления комментария к отчёту.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        
        // Проверяем, что комментарий не пустой
        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            // Отправляем сообщение о том, что комментарий пустой
            await client.SendMessage(
                chatId: request.ChatId,
                text: EmptyCommentMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Получаем отчёт, который проверяет пользователь
        var report = await unitOfWork.Query<Report>()
            .FirstOrDefaultAsync(r => r.Id == request.User!.ReviewingReportId, cancellationToken);

        // Если отчёт не найден
        if (report == null)
        {
            // Устанавливаем администратору состояние Idle
            request.User!.State = State.Idle;

            // Удаляем идентификатор проверяемого отчёта
            request.User.ReviewingReportId = null;

            // Сохраняем изменения в базе данных
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Отправляем сообщение о том, что отчёт не найден
            await client.SendMessage(
                chatId: request.ChatId,
                text: ReportNotFoundMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );

            // Завершаем выполнение метода
            return;
        }

        // Если это не сегодняшний отчёт
        if (report.Date.Date != dateTimeProvider.Now.Date)
        {
            // Устанавливаем администратору состояние Idle
            request.User!.State = State.Idle;

            // Удаляем идентификатор проверяемого отчёта
            request.User.ReviewingReportId = null;
            
            // Сохраняем изменения в базе данных
            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Отправляем сообщение о том, что это не сегодняшний отчёт
            await client.SendMessage(
                chatId: request.ChatId,
                text: ReportAlreadyExportedMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );

            // Завершаем выполнение метода
            return;
        }

        // Формируем новый комментарий
        var newComment = $"— {request.Comment}";

        // Если у отчёта уже есть комментарий, добавляем два пропуска строки и новый комментарий
        if (!string.IsNullOrEmpty(report.Comment))
        {
            newComment = $"{report.Comment}\n\n{newComment}";
        }

        // Проверяем, не превышает ли длина комментария 1500 символов
        if (newComment.Length > 1500)
        {
            // Отправляем сообщение о превышении длины комментария
            await client.SendMessage(
                chatId: request.ChatId,
                text: CommentTooLongMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Обновляем комментарий отчёта
        report.Comment = newComment;

        // Устанавливаем состояние пользователя на Idle
        request.User!.State = State.Idle;

        // Сбрасываем ID проверяемого отчёта
        request.User.ReviewingReportId = null;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение об успешном добавлении комментария
        await client.SendMessage(
            chatId: request.ChatId,
            text: CommentAddedSuccessMessage,
            parseMode: ParseMode.Html,
            cancellationToken: CancellationToken.None
        );
    }
}