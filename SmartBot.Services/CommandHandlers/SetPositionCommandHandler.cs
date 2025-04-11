using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Extensions;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Interfaces.Utils;
using SmartBot.Abstractions.Models.Users;
using SmartBot.Abstractions.Models.WorkingChats;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для валидации и установки должности пользователя.
/// </summary>
/// <summary>
/// Конструктор обработчика команды установки должности.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="dateTimeProvider">Провайдер для работы с текущим временем.</param>
/// <param name="options">Настройки параллелизма для рассылки сообщений.</param>
/// <param name="logger">Логгер.</param>
public class SetPositionCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ParallelOptions options,
    ILogger<StartCommandHandler> logger)
    : IRequestHandler<SetPositionCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется пользователю после успешной установки должности.
    /// </summary>
    private const string SuccessMessage = "Ваша должность успешно сохранена! ✅";

    /// <summary>
    /// Сообщение об ошибке, если должность введена некорректно.
    /// Информирует пользователя о требованиях к формату и длине должности.
    /// </summary>
    private const string ErrorMessage =
        "<b>❌Ошибка:</b> Должность введена некорректно. " +
        "Пожалуйста, введите её заново. Длина должности не должна превышать 100 символов.";

    /// <summary>
    /// Сообщение с инструкцией для ввода утреннего отчёта.
    /// </summary>
    private const string MorningReportMessage =
        "<b>Добрый день! 🌞</b>\n\n" +
        "Пожалуйста, составьте <b>утренний отчёт</b> в формате <b>SMART</b>. Это поможет вам чётко сформулировать задачи на день.\n\n" +
        "<b>Формат SMART:</b>\n" +
        "1. <b>Конкретность (Specific)</b> — что именно нужно сделать?\n" +
        "2. <b>Измеримость (Measurable)</b> — как вы поймёте, что задача выполнена?\n" +
        "3. <b>Достижимость (Achievable)</b> — реально ли выполнить задачу за день?\n" +
        "4. <b>Актуальность (Relevant)</b> — насколько задача важна для ваших целей?\n" +
        "5. <b>Ограниченность по времени (Time-bound)</b> — к какому времени задача должна быть выполнена?\n\n" +
        "<i>Пример:</i>\n" +
        "<code>К 15:00 подготовить отчёт по продажам за прошлый месяц, включающий анализ динамики и рекомендации по улучшению.</code>";

    /// <summary>
    /// Сообщение с напоминанием о временных рамках для сдачи отчётов.
    /// </summary>
    private const string DefaultMessage =
        "<b>Добро пожаловать! 🌇</b>\n\n" +
        "Сейчас не время для сдачи отчёта. Пожалуйста, дождитесь уведомления.\n\n" +
        "<b>Напоминание о времени сдачи отчётов:</b>\n" +
        "1. <b>Утренний отчёт</b> — с 9:00 до 10:00 по МСК.\n" +
        "2. <b>Вечерний отчёт</b> — с 18:00 до 19:00 по МСК.\n\n" +
        "Если у вас есть вопросы или вам нужна помощь, вы всегда можете обратиться к вашему руководителю.\n\n" +
        "<i>Спасибо за понимание! 😊</i>";

    /// <summary>
    /// Шаблон сообщения о новом пользователе для администраторов.
    /// Параметры форматирования:
    /// {0} - Полное имя пользователя (или пустая строка, если недоступно)
    /// {1} - Должность пользователя
    /// </summary>
    private const string NewUserNotificationMessage =
        "👤 <b>Новый пользователь</b>\n\n" +
        "📨 <b>Имя:</b> {0}\n" +
        "💼 <b>Должность:</b> {1}\n\n" +
        "Выберите рабочий чат для него или проигнорируйте это сообщение.";

    /// <summary>
    /// Обрабатывает команду установки должности.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде установки должности.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(SetPositionCommand request, CancellationToken cancellationToken)
    {
        // Валидируем введённую должность
        if (!IsPositionValid(request.Position))
        {
            // Отправляем сообщение об ошибке
            await client.SendMessage(
                chatId: request.ChatId,
                text: ErrorMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
            return;
        }

        // Устанавливаем должность пользователю
        request.User!.Position = request.Position;

        // Устанавливаем новое состояние пользователю
        request.User.State = State.AwaitingReportInput;

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение об успешной установке должности
        await client.SendMessage(
            chatId: request.ChatId,
            text: SuccessMessage,
            cancellationToken: CancellationToken.None
        );

        // Получаем текущее время
        var currentTime = dateTimeProvider.Now;

        // Отправляем сообщение с инструкцией для ввода отчёта
        if (currentTime.IsWorkingPeriod())
        {
            await client.SendMessage(
                chatId: request.ChatId,
                text: MorningReportMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }

        // Отправляем сообщение с напоминанием о времени сдачи отчётов
        else
        {
            await client.SendMessage(
                chatId: request.ChatId,
                text: DefaultMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }

        // Получаем список всех чатов из базы данных
        var chats = await unitOfWork
            .Query<WorkingChat>()
            .Select(c => new ValueTuple<long, string>(c.Id, c.Name))
            .Take(100)
            .ToArrayAsync(CancellationToken.None);

        // Проверяем, есть ли чаты для удаления
        if (chats.Length == 0) return;

        // Получаем список ID администраторов
        var admins = await unitOfWork
            .Query<User>()
            .Where(u => u.Role == Role.Admin || u.Role == Role.TeleAdmin)
            .Select(u => u.Id)
            .ToListAsync(CancellationToken.None);
        
        // Параллельно отправляем уведомления каждому администратору
        await Parallel.ForEachAsync(admins, options, async (userId, ct) =>
        {
            try
            {
                // Отправляем сообщение администратору.
                await client.SendMessage(
                    chatId: userId,
                    text: string.Format(NewUserNotificationMessage, request.User?.FullName, request.Position),
                    parseMode: ParseMode.Html,
                    replyMarkup: AdminKeyboard.SelectWorkingChatKeyboard(chats, request.TelegramUserId),
                    cancellationToken: ct
                );
            }
            catch (ApiRequestException ex) // Ловим только ошибки Telegram API
            {
                // Логируем ошибку, если не удалось отправить сообщение.
                logger.LogWarning(ex, "Failed to send new user notification to admin {AdminId}.", userId);
            }
        });
    }

    /// <summary>
    /// Валидирует введённую должность.
    /// </summary>
    /// <param name="position">Введённая должность.</param>
    /// <returns>True, если должность корректна, иначе false.</returns>
    private static bool IsPositionValid(string? position)
    {
        // Проверка, что должность не пустая
        if (string.IsNullOrWhiteSpace(position)) return false;

        // Проверка, что длина должности не превышает 100 символов
        return position.Length <= 100;
    }
}