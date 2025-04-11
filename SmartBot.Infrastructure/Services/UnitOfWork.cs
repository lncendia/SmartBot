using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Infrastructure.Contexts;

namespace SmartBot.Infrastructure.Services;

/// <summary>
/// Реализация интерфейса IUnitOfWork для работы с базой данных.
/// </summary>
/// <param name="context">Контекст базы данных.</param>
public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    /// <summary>
    /// Возвращает IQueryable для сущности типа T.
    /// </summary>
    /// <typeparam name="T">Тип сущности.</typeparam>
    /// <returns>IQueryable для сущности типа T.</returns>
    public IQueryable<T> Query<T>() where T : class
    {
        // Возвращаем DbSet для сущности типа T
        return context.Set<T>();
    }

    /// <summary>
    /// Асинхронно добавляет сущность в базу данных.
    /// </summary>
    /// <typeparam name="T">Тип сущности.</typeparam>
    /// <param name="entity">Сущность для добавления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        // Добавляем сущность в DbSet
        await context.Set<T>().AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Асинхронно добавляет коллекцию сущностей в базу данных.
    /// </summary>
    /// <typeparam name="T">Тип сущности.</typeparam>
    /// <param name="entities">Коллекция сущностей для добавления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        // Добавляем коллекцию сущностей в DbSet
        await context.Set<T>().AddRangeAsync(entities, cancellationToken);
    }

    /// <summary>
    /// Асинхронно удаляет сущность из базы данных.
    /// </summary>
    /// <typeparam name="T">Тип сущности.</typeparam>
    /// <param name="entity">Сущность для удаления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        // Удаляем сущность из DbSet
        context.Set<T>().Remove(entity);
        await Task.CompletedTask; // Для асинхронной совместимости
    }

    /// <summary>
    /// Асинхронно удаляет коллекцию сущностей из базы данных.
    /// </summary>
    /// <typeparam name="T">Тип сущности.</typeparam>
    /// <param name="entities">Коллекция сущностей для удаления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task DeleteRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        // Удаляем коллекцию сущностей из DbSet
        context.Set<T>().RemoveRange(entities);
        await Task.CompletedTask; // Для асинхронной совместимости
    }

    /// <summary>
    /// Асинхронно сохраняет изменения в базе данных.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Сохраняем изменения в базе данных
        await context.SaveChangesAsync(cancellationToken);
    }
}