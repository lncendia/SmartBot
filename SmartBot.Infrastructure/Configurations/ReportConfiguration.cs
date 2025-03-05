using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartBot.Abstractions.Models;

namespace SmartBot.Infrastructure.Configurations;

/// <summary>
/// Конфигурация Fluent API для сущности Report.
/// </summary>
public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    /// <summary>
    /// Настраивает таблицу отчётов.
    /// </summary>
    /// <param name="builder">Строитель для настройки таблицы.</param>
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        // Настраиваем таблицу
        builder.ToTable("Reports");
        
        // Устанавливаем первичный ключ
        builder.HasKey(r => r.Id);
        
        // Устанавливаем ограничения для свойства Comment
        builder.Property(r => r.Comment)
            .HasMaxLength(1500); // Максимальная длина 1500 символов

        // Настройка владеемых типов (owned types) для MorningReport и EveningReport.
        // Это позволяет хранить эти объекты как часть сущности Report в базе данных.
        builder.OwnsOne(b => b.MorningReport, r =>
        {
            // Максимальная длина 5000 символов
            r.Property(userReport => userReport.Data).HasMaxLength(5000);
        });
        
        // Настройка владеемого типа для вечернего отчёта.
        builder.OwnsOne(b => b.EveningReport, r =>
        {
            // Максимальная длина 5000 символов
            r.Property(userReport => userReport.Data).HasMaxLength(5000);
        });

        // Настраиваем связь many-to-one с сущностью User
        builder.HasOne(r => r.User) // У отчёта может быть только один пользователь
            .WithMany(u => u.Reports) // У пользователя может быть много отчётов
            .HasForeignKey(r => r.UserId) // Внешний ключ в Report
            .OnDelete(DeleteBehavior.Cascade); // Каскадное удаление
    }
}