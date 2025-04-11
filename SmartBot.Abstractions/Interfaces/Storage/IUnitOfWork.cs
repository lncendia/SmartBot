namespace SmartBot.Abstractions.Interfaces.Storage;

/// <summary>
/// Интерфейс для работы с единицей работы (Unit of Work)
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Получает запрос для сущности типа T
    /// </summary>
    /// <typeparam name="T">Тип сущности</typeparam>
    /// <returns>Запрос для сущности типа T</returns>
    IQueryable<T> Query<T>() where T : class;
    
    /// <summary>
    /// Асинхронно добавляет сущность типа T
    /// </summary>
    /// <typeparam name="T">Тип сущности</typeparam>
    /// <param name="entity">Сущность для добавления</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Задача, представляющая асинхронную операцию добавления сущности</returns>
    Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Асинхронно добавляет диапазон сущностей типа T
    /// </summary>
    /// <typeparam name="T">Тип сущности</typeparam>
    /// <param name="entities">Диапазон сущностей для добавления</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Задача, представляющая асинхронную операцию добавления диапазона сущностей</returns>
    Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Асинхронно удаляет сущность типа T
    /// </summary>
    /// <typeparam name="T">Тип сущности</typeparam>
    /// <param name="entity">Сущность для удаления</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Задача, представляющая асинхронную операцию удаления сущности</returns>
    Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Асинхронно удаляет диапазон сущностей типа T
    /// </summary>
    /// <typeparam name="T">Тип сущности</typeparam>
    /// <param name="entities">Диапазон сущностей для удаления</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    Task DeleteRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Асинхронно сохраняет изменения в базе данных
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Задача, представляющая асинхронную операцию сохранения изменений</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}