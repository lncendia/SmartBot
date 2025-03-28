using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Models.WorkingChats;
using SmartBot.Services.Extensions;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для начала удаления чата.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class StartRemoveWorkingChatCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork)
    : IRequestHandler<StartRemoveWorkingChatCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется сотруднику для выбора чата для удаления.
    /// </summary>
    private const string AwaitingChannelSelectionMessage =
        "<b>📝 Выберите чат для удаления:</b>\n\n" +
        "Пожалуйста, выберите чат из списка ниже.";

    /// <summary>
    /// Обрабатывает команду начала удаления чата.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartRemoveWorkingChatCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли пользователь администратором
        if (!await request.CheckAdminAsync(client, cancellationToken)) return;

        // Получаем список всех чатов из базы данных
        var channels = await unitOfWork
            .Query<WorkingChat>()
            .Select(c => new ValueTuple<long, string>(c.Id, c.Name))
            .Take(100)
            .ToArrayAsync(cancellationToken);

        // Проверяем, есть ли чаты для удаления
        if (channels.Length == 0)
        {
            // Если чатов нет, отправляем сообщение об ошибке
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: "❌ Нет доступных чатов.",
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }
        
        try
        {
            // Изменяем сообщение с кнопками для выбора чата
            await client.EditMessageText(
                chatId: request.ChatId,
                messageId: request.MessageId,
                text: AwaitingChannelSelectionMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.DeleteChatKeyboard(channels),
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException)
        {
            // Если редактирование не удалось, отправляем новое сообщение
            await client.SendMessage(
                chatId: request.ChatId,
                text: AwaitingChannelSelectionMessage,
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboard.GoBackKeyboard,
                cancellationToken: cancellationToken
            );
        }
    }
}