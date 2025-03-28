using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartBot.Abstractions.Models.Reports;
using SmartBot.Abstractions.Models.Users;
using SmartBot.Abstractions.Models.WorkingChats;

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
            .HasMaxLength(150); // Максимальная длина 150 символов

        // Устанавливаем ограничения для свойства Position
        builder.Property(u => u.Position)
            .HasMaxLength(50); // Максимальная длина 50 символов

        // Настраиваем связь one-to-many с сущностью Report
        builder.HasMany(u => u.Reports) // У пользователя может быть много отчётов
            .WithOne(r => r.User) // У отчёта может быть только один пользователь
            .HasForeignKey(r => r.UserId) // Внешний ключ в Report
            .OnDelete(DeleteBehavior.Cascade); // Каскадное удаление

        // Настраиваем связь many-to-one с сущностью WorkingChat
        builder.HasOne<WorkingChat>() // У пользователя может быть только один рабочий чат
            .WithMany() // У рабочего чата может быть много пользователей
            .HasForeignKey(u => u.WorkingChatId) // Внешний ключ в User
            .OnDelete(DeleteBehavior.SetNull); // Установка внешнего ключа в null при удалении отчёта

        // Настраиваем связь many-to-one с сущностью WorkingChat
        builder.HasOne<WorkingChat>() // У пользователя может быть только один рабочий чат
            .WithMany() // У рабочего чата может быть много пользователей
            .HasForeignKey(u => u.SelectedWorkingChatId) // Внешний ключ в User
            .OnDelete(DeleteBehavior.SetNull); // Установка внешнего ключа в null при удалении отчёта

        builder.OwnsOne(u => u.AnswerFor, answerBuilder =>
        {
            answerBuilder.ToTable("AnswersFor");
            
            answerBuilder.HasOne<Report>()
                .WithMany()
                .HasForeignKey(a => a.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            answerBuilder.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.ToUserId)
                .OnDelete(DeleteBehavior.Cascade);

            answerBuilder.Property(a => a.Message)
                .HasMaxLength(2000); // Максимальная длина 2000 символов
        });

        builder.OwnsOne(u => u.ReviewingReport, reviewingBuilder =>
        {
            reviewingBuilder.ToTable("ReviewingReports");
            
            // Настраиваем связь many-to-one с сущностью Report
            reviewingBuilder.HasOne<Report>() // У пользователя может быть только один отчёт на проверке
                .WithMany() // У отчёта может быть много администраторов
                .HasForeignKey(u => u.ReportId) // Внешний ключ в User
                .OnDelete(DeleteBehavior.Cascade); // Установка внешнего ключа в null при удалении отчёта
        });
    }
}