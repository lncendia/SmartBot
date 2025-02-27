using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartBot.Abstractions.Models;

namespace SmartBot.Infrastructure.Configurations;

/// <summary>
/// Конфигурация Fluent API для сущности User.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// Настраивает таблицу пользователей.
    /// </summary>
    /// <param name="builder">Строитель для настройки таблицы.</param>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Настраиваем таблицу
        builder.ToTable("Users");
        
        // Устанавливаем первичный ключ
        builder.HasKey(u => u.Id);

        // Устанавливаем ограничения для свойства FullName
        builder.Property(u => u.FullName)
            .HasMaxLength(150); // Максимальная длина 100 символов

        // Устанавливаем ограничения для свойства Position
        builder.Property(u => u.Position)
            .HasMaxLength(100); // Максимальная длина 50 символов

        // Настраиваем связь one-to-many с сущностью Report
        builder.HasMany(u => u.Reports) // У пользователя может быть много отчётов
            .WithOne(r => r.User) // У отчёта может быть только один пользователь
            .HasForeignKey(r => r.UserId) // Внешний ключ в Report
            .OnDelete(DeleteBehavior.Cascade); // Каскадное удаление

        // Настраиваем связь many-to-one с сущностью Report
        builder.HasOne<Report>() // У пользователя может быть только один отчёт на проверке
            .WithMany() // У отчёта может быть много проверяющих
            .HasForeignKey(u => u.ReviewingReportId) // Внешний ключ в User
            .OnDelete(DeleteBehavior.SetNull); // Установка внешнего ключа в null при удалении отчёта
    }
}