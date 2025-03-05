using MediatR;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для валидации и установки ФИО пользователя.
/// </summary>
/// <summary>
/// Конструктор обработчика команды установки ФИО.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class SetFullNameCommandHandler(ITelegramBotClient client, IUnitOfWork unitOfWork)
    : IRequestHandler<SetFullNameCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется пользователю после успешной установки ФИО.
    /// </summary>
    private const string SuccessMessage =
        "Ваше ФИО успешно сохранено! ✅\nТеперь введите свою должность.";

    /// <summary>
    /// Сообщение об ошибке, если ФИО введено некорректно.
    /// Информирует пользователя о требованиях к формату и длине ФИО.
    /// </summary>
    private const string ErrorMessage =
        "<b>❌Ошибка:</b> ФИО введено некорректно. " +
        "Пожалуйста, введите его в формате: <b>Иванов Иван Иванович</b>. " +
        "Длина ФИО не должна превышать 150 символов.";

    /// <summary>
    /// Обрабатывает команду установки ФИО.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде установки ФИО.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(SetFullNameCommand request, CancellationToken cancellationToken)
    {
        // Валидируем введённое ФИО
        if (!IsFullNameValid(request.FullName))
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

        // Устанавливаем ФИО пользователю
        request.User!.FullName = request.FullName;

        // Устанавливаем новое состояние пользователю
        request.User.State = State.AwaitingPositionInput;

        // Отправляем сообщение об успешной установке ФИО
        await client.SendMessage(
            chatId: request.ChatId,
            text: SuccessMessage,
            cancellationToken: cancellationToken
        );
        
        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Валидирует введённое ФИО.
    /// </summary>
    /// <param name="fullName">Введённое ФИО.</param>
    /// <returns>True, если ФИО корректно, иначе false.</returns>
    private static bool IsFullNameValid(string? fullName)
    {
        // Проверка, что ФИО не пустое
        if (string.IsNullOrEmpty(fullName)) return false;

        // Проверка, что длина ФИО не превышает 150 символов
        if (fullName.Length > 150) return false;

        // Разделение ФИО на части (Фамилия, Имя, Отчество)
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Проверка, что ФИО состоит из трёх частей
        if (parts.Length != 3) return false;

        // Проверка каждой части ФИО
        foreach (var part in parts)
        {
            // Проверка, что часть не пустая
            if (string.IsNullOrWhiteSpace(part)) return false;

            // Проверка, что все символы в части являются буквами
            if (!part.All(char.IsLetter)) return false;

            // Проверка, что часть начинается с заглавной буквы
            if (!char.IsUpper(part[0])) return false;
        }

        // Если все проверки пройдены, возвращаем true
        return true;
    }
}
