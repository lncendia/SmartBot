using Microsoft.EntityFrameworkCore;
using SmartBot.Abstractions.Models.Reports;
using SmartBot.Abstractions.Models.Users;
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
        
        // Применяем конфигурацию для сущности WorkingChat
        modelBuilder.ApplyConfiguration(new WorkingChatConfiguration());
        
        // Применяем конфигурацию для сущности UserReport
        modelBuilder.ApplyConfiguration(new UserReportConfiguration());
        
        // Автозагрузка связанных отчётов
        modelBuilder.Entity<Report>()
            .Navigation(r => r.MorningReport)
            .AutoInclude();
    
        // Автозагрузка связанных отчётов
        modelBuilder.Entity<Report>()
            .Navigation(r => r.EveningReport)
            .AutoInclude();
        
        // Автозагрузка связанных чатов
        modelBuilder.Entity<User>()
            .Navigation(r => r.WorkingChat)
            .AutoInclude();
        
        // Вызываем базовую реализацию метода
        base.OnModelCreating(modelBuilder);
    }
}
