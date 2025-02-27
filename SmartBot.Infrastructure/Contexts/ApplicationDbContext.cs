using Microsoft.EntityFrameworkCore;
using SmartBot.Infrastructure.Configurations;

namespace SmartBot.Infrastructure.Contexts;

/// <summary>
/// Контекст базы данных для приложения.
/// </summary>
/// <param name="options">Опции конфигурации для DbContext.</param>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Конфигурирует модель базы данных.
    /// </summary>
    /// <param name="modelBuilder">Объект для построения модели базы данных.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Применяем конфигурацию для сущности User
        modelBuilder.ApplyConfiguration(new UserConfiguration());

        // Применяем конфигурацию для сущности Report
        modelBuilder.ApplyConfiguration(new ReportConfiguration());
        
        // Применяем конфигурацию для сущности Exporter
        modelBuilder.ApplyConfiguration(new ExporterConfiguration());
        
        // Вызываем базовую реализацию метода
        base.OnModelCreating(modelBuilder);
    }
}
