using MediatR;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = SmartBot.Abstractions.Models.Users.User;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды /start для бота.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class StartCommandHandler(ITelegramBotClient client, IUnitOfWork unitOfWork) : IRequestHandler<StartCommand>
{
    /// <summary>
    /// Стартовый стикер, который отправляется пользователю.
    /// </summary>
    private static readonly InputFile StartSticker =
        new InputFileId("CAACAgIAAxkBAAEN6a1nwOKjmgJYCqSZDd-wYvIWjMQyMgACjSgAAn-2SEvDXtidbUUHOjYE");

    /// <summary>
    /// Стартовое сообщение, которое отправляется пользователю.
    /// </summary>
    private const string StartMessage =
        "<b>👋 Добро пожаловать!</b>\n\n" +
        "Я — ваш помощник в составлении <b>SMART-отчётов</b>. С моей помощью вы сможете легко и эффективно планировать свои задачи и подводить итоги дня.\n\n" +
        "📌 <b>Для начала работы, пожалуйста, введите ваше <i>ФИО</i> в именительном падеже.</b>\n\n" +
        "<i>Пример:</i> <code>Иванов Иван Иванович</code>\n\n" +
        "Если у вас возникнут вопросы, обратитесь к своему руководителю, он подскажет, как работать с ботом. 😊";

    /// <summary>
    /// Обрабатывает команду /start.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде /start.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartCommand request, CancellationToken cancellationToken)
    {
        // Создаем нового пользователя
        var user = new User { Id = request.TelegramUserId };

        // Добавляем пользователя в базу данных
        await unitOfWork.AddAsync(user, cancellationToken);

        // Отправляем стартовый стикер
        await client.SendSticker(
            chatId: request.ChatId,
            sticker: StartSticker,
            cancellationToken: cancellationToken
        );

        // Отправляем стартовое сообщение
        await client.SendMessage(
            chatId: request.ChatId,
            text: StartMessage,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );

        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}