using Telegram.Bot.Types;

namespace SmartBot.Abstractions.Interfaces.UpdateHandler;

/// <summary>
/// Интерфейс для обработки обновлений
/// </summary>
public interface IUpdateHandler
{
    /// <summary>
    /// Обрабатывает обновление
    /// </summary>
    /// <param name="update">Обновление</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Задача, представляющая асинхронную операцию обработки обновления</returns>
    public Task HandleAsync(Update update, CancellationToken cancellationToken = default);
}