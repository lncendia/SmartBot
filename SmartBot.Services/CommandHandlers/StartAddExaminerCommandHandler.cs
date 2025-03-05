using MediatR;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Services.Keyboards.ExaminerKeyboard;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для начала процесса добавления нового проверяющего.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
public class StartAddExaminerCommandHandler(ITelegramBotClient client, IUnitOfWork unitOfWork)
    : IRequestHandler<StartAddExaminerCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется, если пользователь не является проверяющим.
    /// </summary>
    private const string NotExaminerMessage =
        "<b>❌ Ошибка:</b> Вы не являетесь проверяющим. Только проверяющие могут добавлять новых проверяющих.";

    /// <summary>
    /// Сообщение с запросом на ввод ID нового проверяющего.
    /// </summary>
    private const string AwaitingExaminerIdMessage =
        "<b>📝 Введите ID пользователя, которого хотите назначить проверяющим:</b>\n\n" +
        "Пожалуйста, отправьте числовой идентификатор пользователя.";

    /// <summary>
    /// Обрабатывает команду начала процесса добавления нового проверяющего.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(StartAddExaminerCommand request, CancellationToken cancellationToken)
    {
        // Проверяем, является ли пользователь проверяющим
        if (!request.User!.IsExaminer)
        {
            // Устанавливаем проверяющему состояние AwaitingReportInput
            request.User.State = State.AwaitingReportInput;

            // Сохраняем изменения в базе данных
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Отправляем сообщение о том, что пользователь не является проверяющим
            await client.SendMessage(
                chatId: request.ChatId,
                text: NotExaminerMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Устанавливаем состояние пользователя на AwaitingExaminerIdForAdding
        request.User.State = State.AwaitingExaminerIdForAdding;
        
        // Сохраняем изменения в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Отправляем сообщение с запросом на ввод ID нового проверяющего
        await client.SendMessage(
            chatId: request.ChatId,
            text: AwaitingExaminerIdMessage,
            parseMode: ParseMode.Html,
            replyMarkup: ExamKeyboard.GoBackKeyboard,
            cancellationToken: cancellationToken
        );
    }
}