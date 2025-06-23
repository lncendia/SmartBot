using System.Collections.Concurrent;
using SmartBot.Abstractions.Interfaces.Utils;

namespace SmartBot.Services.Services;

/// <summary>
/// Сервис синхронизации пользователей.
/// </summary>
public class UserSynchronizationService : IUserSynchronizationService, IDisposable
{
    /// <summary>
    /// Словарь для хранения семафоров по идентификаторам пользователей.
    /// </summary>
    private readonly ConcurrentDictionary<long, SemaphoreSlim> _userSemaphores = new();

    /// <summary>
    /// Синхронизирует выполнение операций для указанного пользователя.
    /// Если для пользователя уже выполняется синхронизация, метод будет ждать её завершения.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task SynchronizeAsync(long userId, CancellationToken cancellationToken = default)
    {
        // Проверяем, существует ли семафор для данного пользователя
        if (_userSemaphores.TryGetValue(userId, out var semaphore))
        {
            // Ждем, пока семафор освободится
            await semaphore.WaitAsync(cancellationToken);
        }
        
        // Создаём новый семафор для данного пользователя
        _userSemaphores[userId] = new SemaphoreSlim(1, 1);
        
        // Захватываем созданный семафор
        await _userSemaphores[userId].WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Освобождает ресурсы, связанные с синхронизацией для указанного пользователя.
    /// Удаляет семафор из словаря, если он больше не используется.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    public void Release(long userId)
    {
        // Проверяем, существует ли семафор для данного пользователя
        if (!_userSemaphores.TryRemove(userId, out var semaphore)) return;
        
        // Освобождаем семафор
        semaphore.Release();
            
        // Освобождаем ресурсы семафора
        semaphore.Dispose();
    }

    /// <summary>
    /// Освобождает все ресурсы, связанные с синхронизацией.
    /// </summary>
    public void Dispose()
    {
        // Освобождаем все ресурсы семафоров
        foreach (var semaphore in _userSemaphores.Values) semaphore.Dispose();

        // Подавляем финализацию
        GC.SuppressFinalize(this);
    }
}