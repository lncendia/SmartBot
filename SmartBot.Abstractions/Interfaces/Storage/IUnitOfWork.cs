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
    /// Асинхронно добавляет сущность в контекст данных для последующего сохранения.
    /// </summary>
    /// <typeparam name="T">Тип добавляемой сущности. Должен быть ссылочным типом (class).</typeparam>
    /// <param name="entity">Добавляемая сущность. Не может быть null.</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <remarks>
    /// Сущность будет добавлена в состояние Added и сохранится в базе данных
    /// при следующем вызове SaveChangesAsync().
    /// </remarks>
    Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Помечает сущность как измененную для последующего обновления в базе данных.
    /// </summary>
    /// <typeparam name="T">Тип обновляемой сущности. Должен быть ссылочным типом (class).</typeparam>
    /// <param name="entity">Обновляемая сущность. Не может быть null.</param>
    /// <remarks>
    /// Для отслеживаемых (tracked) сущностей обычно не требуется явно вызывать Update(),
    /// так как изменения автоматически обнаруживаются.
    /// </remarks>
    void Update<T>(T entity) where T : class;

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