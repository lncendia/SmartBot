using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartBot.Infrastructure.Contexts;

namespace SmartBot.Infrastructure.DatabaseInitialization;

/// <summary>
/// Класс для инициализации начальных данных в базу данных
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Асинхронная инициализация базы данных с начальными данными.
    /// </summary>
    /// <param name="scopeServiceProvider">Провайдер сервисов для создания области видимости.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public static async Task InitAsync(IServiceProvider scopeServiceProvider)
    {
        // Получаем экземпляр контекста базы данных (ApplicationDbContext) из провайдера сервисов.
        var context = scopeServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Выполняем миграции
        await context.Database.MigrateAsync();
    }
}