using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartBot.Abstractions.Models;

namespace SmartBot.Infrastructure.Configurations;

/// <summary>
/// Конфигурация Fluent API для сущности WorkingChat.
/// </summary>
public class WorkingChatConfiguration : IEntityTypeConfiguration<WorkingChat>
{
    /// <summary>
    /// Настраивает таблицу рабочих чатов.
    /// </summary>
    /// <param name="builder">Строитель для настройки таблицы.</param>
    public void Configure(EntityTypeBuilder<WorkingChat> builder)
    {
        // Настраиваем таблицу
        builder.ToTable("WorkingChats");
        
        // Устанавливаем первичный ключ
        builder.HasKey(u => u.Id);

        // Устанавливаем ограничения для свойства Name
        builder.Property(u => u.Name)
            .HasMaxLength(150); // Максимальная длина 150 символов
    }
}