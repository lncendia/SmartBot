using MediatR;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Extensions;
using SmartBot.Abstractions.Interfaces;
using Telegram.Bot;
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
public class SetPositionCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
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

        // Отправляем сообщение об успешной установке должности
        await client.SendMessage(
            chatId: request.ChatId,
            text: SuccessMessage,
            cancellationToken: cancellationToken
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
                cancellationToken: cancellationToken
            );
        }

        // Отправляем сообщение с напоминанием о времени сдачи отчётов
        else
        {
            await client.SendMessage(
                chatId: request.ChatId,
                text: DefaultMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);
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