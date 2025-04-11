using MediatR;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Services.Extensions;
using Telegram.Bot;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для возврата пользователя в исходное состояние
/// </summary>
/// <remarks>
/// Выполняет сброс текущего состояния пользователя:
/// - Для сотрудников: возвращает в состояние ожидания ввода отчета (AwaitingReportInput)
/// - Для администраторов: возвращает в состояние бездействия (Idle)
/// - Сбрасывает контекст ответа на сообщение (AnswerFor)
/// - Удаляет сообщение с кнопкой "Отмена"
/// </remarks>
/// <param name="client">Экземпляр Telegram Bot API клиента</param>
/// <param name="unitOfWork">Unit of Work для работы с базой данных</param>
public class CancelCommandHandler(ITelegramBotClient client, IUnitOfWork unitOfWork)
    : IRequestHandler<CancelCommand>
{
    /// <summary>
    /// Обрабатывает команду возврата в исходное состояние
    /// </summary>
    /// <param name="request">Команда с данными пользователя</param>
    /// <param name="cancellationToken">Токен отмены асинхронной операции</param>
    /// <returns>Task, представляющий асинхронную операцию</returns>
    public async Task Handle(CancelCommand request, CancellationToken cancellationToken)
    {
        // Определяем новое состояние пользователя на основе его роли
        // Сотрудники возвращаются в состояние ожидания отчета, остальные - в состояние покоя
        var newState = request.User!.IsEmployee
            ? State.AwaitingReportInput
            : State.Idle;

        // Устанавливаем новое состояние пользователя
        request.User.State = newState;
        
        // Сбрасываем контекст проверяемого отчёта
        request.User.ReviewingReport = null;

        // Очищаем контекст ответа на предыдущее сообщение
        request.User.AnswerFor = null;
        
        // Сохраняем изменения состояния пользователя в базе данных
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Удаляем сообщение с кнопкой "Отмена" из чата
        await request.TryDeleteMessageAsync(client, CancellationToken.None);
    }
}