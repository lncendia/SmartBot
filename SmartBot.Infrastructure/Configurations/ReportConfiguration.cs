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

        // Устанавливаем ограничения для свойства Content
        builder.Property(r => r.MorningReport)
            .HasMaxLength(5000); // Максимальная длина 1000 символов

        // Устанавливаем ограничения для свойства Content
        builder.Property(r => r.EveningReport)
            .HasMaxLength(5000); // Максимальная длина 1000 символов

        // Настраиваем связь many-to-one с сущностью User
        builder.HasOne(r => r.User) // У отчёта может быть только один пользователь
            .WithMany(u => u.Reports) // У пользователя может быть много отчётов
            .HasForeignKey(r => r.UserId) // Внешний ключ в Report
            .OnDelete(DeleteBehavior.Cascade); // Каскадное удаление
    }
}