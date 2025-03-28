using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Models.WorkingChats;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для добавления нового чата.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class AddWorkingChatCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddWorkingChatCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если чат уже существует.
    /// </summary>
    private const string ChatAlreadyExistsMessage =
        "<b>❌ Ошибка:</b> Рабочий чат с таким идентификатором уже существует.";

    /// <summary>
    /// Сообщение об успешном добавлении чата.
    /// </summary>
    private const string ChatAddedSuccessMessage =
        "<b>✅ Рабочий чат успешно добавлен!</b>";

    /// <summary>
    /// Обрабатывает команду добавления нового чата.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(AddWorkingChatCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Добавляем новый чат в базу данных
            await unitOfWork.AddAsync(new WorkingChat
            {
                Id = request.WorkingChatId,
                Name = TruncateWithEllipsis(request.WorkingChatName)
            }, cancellationToken);

            // Сохраняем изменения в базе данных
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Отправляем сообщение об успешном добавлении чата
            await client.SendMessage(
                chatId: request.ChatId,
                text: ChatAddedSuccessMessage,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }
        catch (DbUpdateException)
        {
            // Если чат уже существует, отправляем сообщение об ошибке
            await client.SendMessage(
                chatId: request.ChatId,
                text: ChatAlreadyExistsMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: CancellationToken.None
            );
        }
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