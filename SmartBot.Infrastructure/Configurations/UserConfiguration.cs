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

        // Устанавливаем ограничения для свойства CurrentReport
        builder.Property(u => u.CurrentReport)
            .HasMaxLength(5000); //Максимальная длина 5000 символов

        // Настраиваем связь one-to-many с сущностью Report
        builder.HasMany(u => u.Reports) // У пользователя может быть много отчётов
            .WithOne(r => r.User) // У отчёта может быть только один пользователь
            .HasForeignKey(r => r.UserId) // Внешний ключ в Report
            .OnDelete(DeleteBehavior.Cascade); // Каскадное удаление

        // Настраиваем связь many-to-one между User и WorkingChat
        builder.HasOne(u => u.WorkingChat) // У пользователя может быть только один рабочий чат
            .WithMany() // У рабочего чата может быть много пользователей
            .HasForeignKey(u => u.WorkingChatId) // Внешний ключ в таблице User
            .OnDelete(DeleteBehavior.SetNull); // При удалении чата у пользователей WorkingChatId станет null

        // Настраиваем связь many-to-one между User и временно выбранным WorkingChat
        builder.HasOne<WorkingChat>() // У пользователя может быть только один временно выбранный чат
            .WithMany() // У рабочего чата может быть много пользователей с временным выбором
            .HasForeignKey(u => u.SelectedWorkingChatId) // Внешний ключ в таблице User
            .OnDelete(DeleteBehavior.SetNull); // При удалении чата SelectedWorkingChatId станет null
        
        // Настраиваем связь many-to-one между User и временно выбранным User
        builder.HasOne<User>() // У пользователя может быть только один временно выбранный пользователь
            .WithMany() // У пользователя может быть много пользователей с временным выбором
            .HasForeignKey(u => u.SelectedUserId) // Внешний ключ в таблице User
            .OnDelete(DeleteBehavior.SetNull); // При удалении пользователя SelectedUserId станет null

        // Настраиваем owned-тип AnswerFor (ответ на сообщение)
        builder.OwnsOne(u => u.AnswerFor, answerBuilder =>
        {
            // Храним в отдельной таблице
            answerBuilder.ToTable("AnswersFor");

            // Связь с отчетом, на который дается ответ
            answerBuilder.HasOne<Report>()
                .WithMany()
                .HasForeignKey(a => a.ReportId)
                .OnDelete(DeleteBehavior.Cascade); // Каскадное удаление при удалении отчета

            // Связь с пользователем, которому адресован ответ
            answerBuilder.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.ToUserId)
                .OnDelete(DeleteBehavior.Cascade); // Каскадное удаление при удалении пользователя

            // Ограничение длины текста ответа
            answerBuilder.Property(a => a.Message)
                .HasMaxLength(2000); // Максимальная длина соответствует ограничениям Telegram
        });

        // Настраиваем owned-тип ReviewingReport (проверяемый отчет)
        builder.OwnsOne(u => u.ReviewingReport, reviewingBuilder =>
        {
            // Храним в отдельной таблице
            reviewingBuilder.ToTable("ReviewingReports");

            // Связь с проверяемым отчетом
            reviewingBuilder.HasOne<Report>()
                .WithMany()
                .HasForeignKey(u => u.ReportId)
                .OnDelete(DeleteBehavior.Cascade); // Каскадное удаление при удалении отчета
        });
    }
}