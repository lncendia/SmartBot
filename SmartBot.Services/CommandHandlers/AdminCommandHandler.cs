using MediatR;
using SmartBot.Abstractions.Commands;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для отображения административной панели управления
/// </summary>
/// <param name="client">Экземпляр Telegram Bot API клиента</param>
public class AdminCommandHandler(ITelegramBotClient client)
    : IRequestHandler<AdminCommand>
{
    /// <summary>
    /// Сообщение об отсутствии прав администратора
    /// </summary>
    private const string NotAdminMessage =
        "<b>🔐 Доступ запрещен</b>\n\n" +
        "У вас нет прав администратора для доступа к этой панели.\n" +
        "Если вы считаете, что это ошибка, обратитесь к главному администратору.";

    /// <summary>
    /// Приветственное сообщение административной панели
    /// </summary>
    private const string AdminPanelMessage =
        "<b>⚙️ Панель управления администратора</b>\n\n" +
        "Выберите действие из меню ниже:";

    /// <summary>
    /// Обрабатывает запрос на отображение административной панели
    /// </summary>
    /// <param name="request">Данные запроса, включая информацию о пользователе</param>
    /// <param name="cancellationToken">Токен для отмены операции</param>
    /// <returns>Task, представляющий асинхронную операцию</returns>
    public async Task Handle(AdminCommand request, CancellationToken cancellationToken)
    {
        // Проверяем наличие прав администратора у пользователя
        if (!request.User!.IsAdmin)
        {
            // Уведомляем пользователя об отсутствии прав доступа
            await client.SendMessage(
                chatId: request.ChatId,
                text: NotAdminMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            return;
        }

        // Отображаем основное меню администратора
        await client.SendMessage(
            chatId: request.ChatId,
            text: AdminPanelMessage,
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboard.MainKeyboard,
            cancellationToken: cancellationToken
        );
    }
}