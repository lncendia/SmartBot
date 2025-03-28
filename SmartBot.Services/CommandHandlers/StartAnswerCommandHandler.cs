using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Models.Reports;
using SmartBot.Abstractions.Models.Users;
using SmartBot.Services.Extensions;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для начала процесса ответа на сообщение пользователя
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram Bot API</param>
/// <param name="unitOfWork">Единица работы для взаимодействия с базой данных</param>
public class StartAnswerCommandHandler(ITelegramBotClient client, IUnitOfWork unitOfWork)
    : IRequestHandler<StartAnswerCommand>
{
    /// <summary>
    /// Сообщение с инструкцией для ответа на сообщение пользователя
    /// </summary>
    private const string AnswerInfoMessage =
        "<b>📨 Ответ на сообщение</b>\n\n" +
        "Введите текст ответа на сообщение:";

    /// <summary>
    /// Сообщение об ошибке, если пользователь не найден
    /// </summary>
    private const string UserNotFoundMessage = "❌ Пользователь не найден.";

    /// <summary>
    /// Сообщение об ошибке, если пользователь заблокирован
    /// </summary>
    private const string UserBlockedMessage = "❌ Пользователь заблокирован.";

    /// <summary>
    /// Сообщение, которое отправляется, если отчёт не найден.
    /// </summary>
    private const string ReportNotFoundMessage = "❌ Отчёт не найден.";
    
    /// <summary>
    /// Сообщение, которое отправляется, если не удалось извлечь оригинальный текст из сообщения.
    /// </summary>
    private const string CannotExtractTextMessage = "❌ Не удалось извлечь текст сообщения.";

    /// <summary>
    /// Обрабатывает команду начала процесса ответа на сообщение
    /// </summary>
    /// <param name="request">Данные входящего запроса</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    public async Task Handle(StartAnswerCommand request, CancellationToken cancellationToken)
    {
        // Извлекаем оригинальный ответ из сообщения
        var message = ExtractLastCommentText(request.Message);
        
        // Если не удалось извлечь ответ
        if (message == null)
        {
            // Уведомляем администратора об неудаче
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: CannotExtractTextMessage,
                cancellationToken: cancellationToken
            );

            // Удаляем сообщение с кнопкой вызова команды
            await request.TryDeleteMessageAsync(client, cancellationToken);

            // Завершаем выполнение метода
            return;
        }
        
        // Получаем отчёт из базы данных по идентификатору
        var report = await unitOfWork.Query<Report>()
            .FirstOrDefaultAsync(m => m.Id == request.ReportId, cancellationToken);

        // Проверяем существование отчёта
        if (report == null)
        {
            // Уведомляем администратора об отсутствии сообщения
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: ReportNotFoundMessage,
                cancellationToken: cancellationToken
            );

            // Удаляем сообщение с кнопкой вызова команды
            await request.TryDeleteMessageAsync(client, cancellationToken);

            // Завершаем выполнение метода
            return;
        }

        // Получаем пользователя-автора сообщения
        var messageAuthor = await unitOfWork.Query<User>()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        // Проверяем существование пользователя
        if (messageAuthor == null)
        {
            // Уведомляем администратора об отсутствии пользователя
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: UserNotFoundMessage,
                cancellationToken: cancellationToken
            );

            // Удаляем сообщение с кнопкой вызова команды
            await request.TryDeleteMessageAsync(client, cancellationToken);

            // Завершаем выполнение метода
            return;
        }

        // Проверяем статус пользователя
        if (messageAuthor.Role == Role.Blocked)
        {
            // Уведомляем администратора о блокировке пользователя
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: UserBlockedMessage,
                cancellationToken: cancellationToken
            );

            // Удаляем сообщение с кнопкой вызова команды
            await request.TryDeleteMessageAsync(client, cancellationToken);

            // Завершаем выполнение метода
            return;
        }

        // Устанавливаем состояние ожидания ввода ответа
        request.User!.State = State.AwaitingAnswerInput;

        // Создаем и устанавливаем данные сообщения для дальнейшего ответа
        request.User.AnswerFor = new AnswerFor
        {
            ToUserId = request.UserId,
            ReportId = request.ReportId,
            EveningReport = request.EveningReport,
            Message = message
        };

        // Фиксируем изменения состояния пользователя в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение с инструкцией для ответа
        await client.SendMessage(
            chatId: request.ChatId,
            text: AnswerInfoMessage,
            parseMode: ParseMode.Html,
            replyMarkup: DefaultKeyboard.CancelKeyboard,
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Извлекает текст последнего комментария из HTML-сообщения
    /// </summary>
    /// <param name="messageText">Текст сообщения в HTML-формате</param>
    /// <returns>
    /// Текст последнего комментария (содержимое последнего тега blockquote),
    /// или null, если blockquote не найден.
    /// </returns>
    /// <remarks>
    /// Работает для обоих форматов сообщений:
    /// 1. С цепочкой комментариев (AnswerMessageFormat)
    /// 2. С одиночным комментарием (CommentMessageFormat)
    /// Всегда возвращает содержимое последнего блока blockquote
    /// </remarks>
    private static string? ExtractLastCommentText(string? messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
            return null;

        // Находим индекс последнего открывающего тега <blockquote>
        var lastQuoteStart = messageText.LastIndexOf("<blockquote>", StringComparison.Ordinal);
        if (lastQuoteStart == -1)
            return null;

        // Находим индекс закрывающего тега </blockquote> после последнего открывающего
        var quoteEnd = messageText.IndexOf("</blockquote>", lastQuoteStart, StringComparison.Ordinal);
        if (quoteEnd == -1)
            return null;

        // Вычисляем начало и длину текста комментария
        var textStart = lastQuoteStart + "<blockquote>".Length;
        var textLength = quoteEnd - textStart;

        // Извлекаем и декодируем текст
        return messageText.Substring(textStart, textLength);
    }
}