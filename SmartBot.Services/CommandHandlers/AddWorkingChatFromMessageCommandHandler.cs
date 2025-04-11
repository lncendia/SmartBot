using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Models.WorkingChats;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для добавления нового чата.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class AddWorkingChatFromMessageCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddWorkingChatFromMessageCommand>
{
    /// <summary>
    /// Сообщение об успешном добавлении нового рабочего чата
    /// </summary>
    private const string ChatAddedSuccessMessage = "<b>✅ Новый рабочий чат успешно добавлен!</b>";

    /// <summary>
    /// Сообщение об успешном обновлении существующего рабочего чата
    /// </summary>
    private const string ChatUpdatedSuccessMessage = "<b>🔄 Рабочий чат успешно обновлен!</b>";

    /// <summary>
    /// Сообщение об отсутствии прав администратора
    /// </summary>
    private const string NotAdminMessage =
        "<b>🔐 Доступ запрещен</b>\n\n" +
        "У вас нет прав администратора для выполнения этой команды.\n" +
        "Если вы считаете, что это ошибка, обратитесь к главному администратору.";

    
    /// <summary>
    /// Обрабатывает команду добавления нового рабочего чата в систему
    /// </summary>
    /// <param name="request">Команда с данными о добавляемом чате</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    public async Task Handle(AddWorkingChatFromMessageCommand request, CancellationToken cancellationToken)
    {
        // Проверяем наличие прав администратора у пользователя
        if (!request.User!.IsAdmin)
        {
            // Уведомляем пользователя об отсутствии прав доступа
            await client.SendMessage(
                replyParameters: new ReplyParameters
                {
                    MessageId = request.MessageId
                },
                chatId: request.ChatId,
                text: NotAdminMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            return;
        }
        
        // Ищем существующий чат в базе данных по ID
        var chat = await unitOfWork.Query<WorkingChat>()
            .FirstOrDefaultAsync(c => c.Id == request.WorkingChatId, cancellationToken: cancellationToken);

        // Флаг, указывающий на создание нового чата
        var isNewChat = false;

        // Если чат не найден - создаем новый
        if (chat == null)
        {
            // Создаем новый экземпляр рабочего чата
            chat = new WorkingChat
            {
                // Устанавливаем ID чата из запроса
                Id = request.WorkingChatId,

                // Устанавливаем ID треда из запроса (может быть null)
                MessageThreadId = request.MessageThreadId,

                // Устанавливаем обрезанное название чата
                Name = TruncateWithEllipsis(request.WorkingChatName)
            };

            // Добавляем новый чат в контекст данных
            await unitOfWork.AddAsync(chat, cancellationToken);

            // Устанавливаем флаг создания нового чата
            isNewChat = true;
        }
        else
        {
            // Обновляем название существующего чата (обрезанное)
            chat.Name = TruncateWithEllipsis(request.WorkingChatName);

            // Обновляем ID треда в существующем чате
            chat.MessageThreadId = request.MessageThreadId;
        }

        // Сохраняем все изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Формируем сообщение о результате операции
        var message = isNewChat
            ? ChatAddedSuccessMessage
            : ChatUpdatedSuccessMessage;

        // Отправляем сообщение с результатом операции
        await client.SendMessage(
            replyParameters: new ReplyParameters
            {
                MessageId = request.MessageId
            },
            chatId: request.ChatId,
            text: message,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Обрезает строку до 20 символов. Если строка длиннее - обрезает до 18 символов и добавляет многоточие.
    /// </summary>
    /// <param name="input">Входная строка</param>
    /// <returns>Обрезанная строка</returns>
    private static string TruncateWithEllipsis(string input)
    {
        const int maxLength = 30;
        const int trimLength = maxLength - 2;
        const string ellipsis = "...";

        if (string.IsNullOrEmpty(input))
            return input;

        return input.Length <= maxLength
            ? input
            : input[..trimLength] + ellipsis;
    }
}