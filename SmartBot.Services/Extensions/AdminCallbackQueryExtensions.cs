using SmartBot.Abstractions.Commands.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace SmartBot.Services.Extensions;

/// <summary>
/// Класс расширений для административных команд Telegram бота
/// </summary>
public static class AdminCallbackQueryExtensions
{
    /// <summary>
    /// Сообщение об ошибке при отсутствии прав администратора
    /// </summary>
    private const string NotAdminMessage = "❌ Вы не администратор.";

    /// <summary>
    /// Проверяет, является ли пользователь администратором
    /// </summary>
    /// <param name="command">Команда callback-запроса</param>
    /// <param name="client">Клиент Telegram Bot API</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>True если пользователь администратор, иначе False</returns>
    public static async Task<bool> CheckAdminAsync(this AdminCallbackQuery command, ITelegramBotClient client, 
        CancellationToken cancellationToken = default)
    {
        // Если пользователь администратор - возвращаем успех
        if (command.User!.IsAdmin) 
            return true;

        // Уведомляем пользователя об отсутствии прав
        await client.AnswerCallbackQuery(
            callbackQueryId: command.CallbackQueryId,
            text: NotAdminMessage,
            cancellationToken: cancellationToken
        );

        // Удаляем сообщение с командой
        await command.TryDeleteMessageAsync(client, cancellationToken);
        
        // Возвращаем отрицательный результат
        return false;
    }

    /// <summary>
    /// Пытается удалить сообщение с командой
    /// </summary>
    /// <param name="command">Команда callback-запроса</param>
    /// <param name="client">Клиент Telegram Bot API</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    public static async Task TryDeleteMessageAsync(this AdminCallbackQuery command, ITelegramBotClient client,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Удаляем сообщение с командой
            await client.DeleteMessage(
                chatId: command.ChatId,
                messageId: command.MessageId,
                cancellationToken: cancellationToken
            );
        }
        catch (ApiRequestException)
        {
            // Игнорируем ошибки удаления сообщения
        }
    }
}