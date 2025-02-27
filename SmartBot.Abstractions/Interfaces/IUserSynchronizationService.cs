namespace SmartBot.Abstractions.Interfaces;

/// <summary>
/// Интерфейс для синхронизации пользователей
/// </summary>
public interface IUserSynchronizationService
{
    /// <summary>
    /// Синхронизирует пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Задача, представляющая асинхронную операцию синхронизации пользователя</returns>
    Task SynchronizeAsync(long userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Освобождает пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    void Release(long userId);
}