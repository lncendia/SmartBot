using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartBot.Abstractions.Models.Reports;

namespace SmartBot.Infrastructure.Configurations;

/// <summary>
/// Конфигурация Fluent API для сущности Report.
/// </summary>
public class UserReportConfiguration : IEntityTypeConfiguration<UserReport>
{
    /// <summary>
    /// Настраивает таблицу отчётов.
    /// </summary>
    /// <param name="builder">Строитель для настройки таблицы.</param>
    public void Configure(EntityTypeBuilder<UserReport> builder)
    {
        // Настраиваем таблицу
        builder.ToTable("ReportElements");

        // Устанавливаем первичный ключ
        builder.HasKey(r => r.Id);

        // Устанавливаем ограничения для свойства Data
        builder.Property(userReport => userReport.Data)
            .HasMaxLength(5000); // Максимальная длина 5000 символов

        // Связь Report -> MorningReport (1-to-1, обязательная)
        builder.HasOne<Report>()
            .WithOne(r => r.MorningReport)
            .HasForeignKey<Report>("MorningReportId")
            .OnDelete(DeleteBehavior.Cascade);

        // Связь Report -> EveningReport (1-to-1, опциональная) 
        builder.HasOne<Report>()
            .WithOne(r => r.EveningReport)
            .HasForeignKey<Report>("EveningReportId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}