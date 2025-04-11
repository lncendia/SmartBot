using MediatR;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Interfaces.Utils;
using SmartBot.Services.Extensions;
using Telegram.Bot;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для отправки отчёта на повторный анализ.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="sender">Отправитель команд.</param>
public class RepeatReportAnalysisCommandHandler(IAsyncSender sender, ITelegramBotClient client)
    : IRequestHandler<RepeatReportAnalysisCommand>
{
    /// <summary>
    /// Сообщение об ошибке, если отчёт пустой или превышает допустимую длину.
    /// </summary>
    private const string EmptyReportErrorMessage = "❌ Этот отчёт уже был отправлен.";
    
    /// <summary>
    /// Обрабатывает команду отправки отчёта на повторный анализ.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде отправки отчёта на повторный анализ.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(RepeatReportAnalysisCommand request, CancellationToken cancellationToken)
    {
        // Если у пользователя нет текущего введенного отчёта
        if (string.IsNullOrWhiteSpace(request.User!.CurrentReport))
        {
            // Уведомляем пользователя о том, что нет текущего отчёта
            await client.AnswerCallbackQuery(
                callbackQueryId: request.CallbackQueryId,
                text: EmptyReportErrorMessage,
                cancellationToken: cancellationToken
            );

            // Удаляем сообщение с командой
            await request.TryDeleteMessageAsync(client, cancellationToken);
            
            // Завершаем выполнение метода
            return;
        }

        // Удаляем сообщение с командой
        await request.TryDeleteMessageAsync(client, cancellationToken);
        
        // Создаем команду для анализа отчёта
        var command = new AnalyzeReportCommand
        {
            ChatId = request.ChatId,
            TelegramUserId = request.TelegramUserId,
            User = request.User,
            MessageId = request.ReportMessageId,
            Report = request.User.CurrentReport
        };
        
        // Отправляем команду на выполнение
        await sender.Send(command, cancellationToken);
    }
}