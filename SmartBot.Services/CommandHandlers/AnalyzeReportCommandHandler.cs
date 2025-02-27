using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Extensions;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Interfaces.ReportAnalyzer;
using SmartBot.Abstractions.Models;
using SmartBot.Services.Keyboards.ExaminerKeyboard;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для анализа и сохранения отчёта.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="reportAnalyzer">Анализатор отчётов.</param>
/// <param name="dateTimeProvider">Провайдер для работы с текущим временем.</param>
/// <param name="logger">Логгер.</param>
public class AnalyzeReportCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    IReportAnalyzer reportAnalyzer,
    IDateTimeProvider dateTimeProvider,
    ILogger<AnalyzeReportCommandHandler> logger)
    : IRequestHandler<AnalyzeReportCommand>
{
    /// <summary>
    /// Сообщение, которое отправляется пользователю после успешного анализа и сохранения утреннего отчёта.
    /// Содержит мотивационное сообщение и напоминание о следующем шаге.
    /// </summary>
    private const string MorningSuccessMessage =
        "<b>Отличный утренний отчёт! ✅</b>\n\n" +
        "Теперь вы готовы к продуктивному дню! Не забывайте следить за своими целями и задачами. " +
        "Вечерний отчёт можно будет отправить после 18:00, чтобы подвести итоги дня.";

    /// <summary>
    /// Сообщение, которое отправляется пользователю после успешного анализа и сохранения вечернего отчёта.
    /// Содержит благодарность за проделанную работу и пожелание хорошего отдыха.
    /// </summary>
    private const string EveningSuccessMessage =
        "<b>Отличный вечерний отчёт! ✅</b>\n\n" +
        "Спасибо за ваш труд и усилия! Желаем вам приятного вечера и хорошего отдыха! 🌙";

    /// <summary>
    /// Сообщение об ошибке, если отчёт некорректен.
    /// Содержит оценку отчёта и рекомендации по его улучшению.
    /// </summary>
    private const string ErrorMessage =
        "<b>Отчёт некорректен (оценка: {0}/10). 🚫</b>\n\n" +
        "Пожалуйста, исправьте отчёт на основе следующих рекомендаций:\n\n" +
        "<b>Рекомендации:</b>\n" +
        "<blockquote>{1}</blockquote>";

    /// <summary>
    /// Сообщение об ошибке, если отчёт пустой или превышает допустимую длину.
    /// Напоминает пользователю о необходимости заполнить отчёт и соблюдать ограничения.
    /// </summary>
    private const string EmptyReportErrorMessage =
        "<b>❌Ошибка:</b> Отчёт не может быть пустым или превышать 5000 символов. " +
        "Пожалуйста, заполните отчёт и убедитесь, что его длина не превышает 5000 символов.";

    /// <summary>
    /// Сообщение, если утренний отчёт уже был отправлен.
    /// Информирует пользователя о том, что утренний отчёт уже зарегистрирован.
    /// </summary>
    private const string MorningReportAlreadySentMessage =
        "<b>⚠️Внимание:</b> Утренний отчёт уже был отправлен. Спасибо за вашу активность!";

    /// <summary>
    /// Сообщение, если вечерний отчёт уже был отправлен.
    /// Информирует пользователя о том, что вечерний отчёт уже зарегистрирован.
    /// </summary>
    private const string EveningReportAlreadySentMessage =
        "<b>⚠️Внимание:</b> Вечерний отчёт уже был отправлен. Спасибо за вашу активность!";

    /// <summary>
    /// Сообщение об ошибке, если анализатор отчётов временно недоступен.
    /// Информирует пользователя о временной проблеме и предлагает повторить попытку позже.
    /// </summary>
    private const string AnalyzerUnavailableMessage =
        "<b>Анализатор отчётов временно недоступен. 🛠️</b>\n\n" +
        "Пожалуйста, повторите попытку через несколько минут. Мы уже работаем над решением проблемы.";

    /// <summary>
    /// Сообщение об ошибке, если попытка отправить отчёт сделана вне допустимого времени.
    /// Информирует пользователя о временных рамках для отправки утренних и вечерних отчётов.
    /// </summary>
    private const string ReportTimeRestrictionMessage =
        "<b>Сейчас не время для отправки отчёта. ⏰</b>\n\n" +
        "Утренние отчёты принимаются с <b>9:00 до 10:00</b> по МСК, " +
        "а вечерние — с <b>18:00 до 19:00</b> по МСК. " +
        "Пожалуйста, вернитесь в указанное время, чтобы отправить отчёт.";

    /// <summary>
    /// Сообщение для проверяющего о новом отчёте пользователя.
    /// Содержит имя пользователя и текст отчёта, а также приглашение оставить комментарий.
    /// </summary>
    private const string ReportSubmissionMessage =
        "📄 <b>Новый отчёт от пользователя:</b> <i>{0}</i>\n\n" +
        "👇 <b>Текст отчёта:</b>\n" +
        "<blockquote>{1}</blockquote>\n\n" +
        "📝 <i>Нажмите на кнопку, если хотите указать замечания или рекомендации для улучшения.</i>";

    /// <summary>
    /// Параметры параллельного выполнения.
    /// </summary>
    private readonly ParallelOptions _parallelOptions = new() { MaxDegreeOfParallelism = 3 };

    /// <summary>
    /// Обрабатывает команду анализа отчёта.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде анализа отчёта.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(AnalyzeReportCommand request, CancellationToken cancellationToken)
    {
        // Валидируем введённый отчёт
        if (!IsReportValid(request.Report))
        {
            // Если отчёт пустой, отправляем сообщение об ошибке
            await client.SendMessage(
                chatId: request.ChatId,
                text: EmptyReportErrorMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Получаем текущую дату и время
        var now = dateTimeProvider.Now;

        // Если текущее время не подходит для отправки утреннего или вечернего отчёта
        if (!now.IsMorningReportTime() && !now.IsEveningReportTime())
        {
            // Отправляем сообщение о временных ограничениях
            await client.SendMessage(
                chatId: request.ChatId,
                text: ReportTimeRestrictionMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Получаем отчёт из базы данных за текущую дату для указанного пользователя
        var report = await unitOfWork.Query<Report>()
            .Where(r => r.UserId == request.TelegramUserId)
            .Where(r => r.Date.Date == now.Date)
            .FirstOrDefaultAsync(cancellationToken);

        // Если текущее время подходит для утреннего отчёта и утренний отчёт уже был отправлен
        if (now.IsMorningReportTime() && report is { MorningReport: not null })
        {
            // Отправляем сообщение о том, что утренний отчёт уже был отправлен
            await client.SendMessage(
                chatId: request.ChatId,
                text: MorningReportAlreadySentMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Если текущее время подходит для вечернего отчёта и вечерний отчёт уже был отправлен
        if (now.IsEveningReportTime() && report is { EveningReport: not null })
        {
            // Отправляем сообщение о том, что вечерний отчёт уже был отправлен
            await client.SendMessage(
                chatId: request.ChatId,
                text: EveningReportAlreadySentMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Объявляем переменную для хранения результата анализа отчёта
        ReportAnalyzeResult analysisResult;

        try
        {
            // Выполняем асинхронный анализ отчёта с использованием анализатора
            var task = reportAnalyzer.AnalyzeAsync(request.Report!, cancellationToken);

            // Пока задача не завершена, отправляем статус "печатает" в чат
            while (!task.IsCompleted)
            {
                await client.SendChatAction(request.ChatId, ChatAction.Typing, cancellationToken: cancellationToken);

                // Ждём 2 секунды перед следующей проверкой
                await Task.Delay(2000, cancellationToken);
            }

            // Получаем результат анализа
            analysisResult = task.Result;
        }
        catch
        {
            // Если произошла ошибка при анализе отчёта, отправляем сообщение об ошибке в чат
            await client.SendMessage(
                chatId: request.ChatId,
                text: AnalyzerUnavailableMessage,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Не продолжаем
            return;
        }

        // Если отчёт некорректен (оценка меньше 4), отправляем рекомендации
        if (analysisResult.Score < 7)
        {
            // Отправляем сообщение с рекомендациями по исправлению отчёта
            await client.SendMessage(
                chatId: request.ChatId,
                text: string.Format(ErrorMessage, analysisResult.Score, analysisResult.Recommendations),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            // Завершаем выполнение метода
            return;
        }

        // Получаем текущую дату и время
        var timeAfterAnalyze = dateTimeProvider.Now;

        // Если отчёт за текущую дату отсутствует, создаём новый
        if (report == null)
        {
            // Создаём новый отчёт с текущей датой и ID пользователя
            report = new Report
            {
                UserId = request.User!.Id,
                Date = timeAfterAnalyze
            };

            // Добавляем отчёт в базу данных
            await unitOfWork.AddAsync(report, cancellationToken);
        }

        // Проверяем текущее время для определения типа отчёта (утренний или вечерний)
        if (now.IsMorningReportTime())
        {
            // Сохраняем отчёт как утренний
            report.MorningReport = request.Report;
        }
        else if (now.IsEveningReportTime())
        {
            // Сохраняем отчёт как вечерний
            report.EveningReport = request.Report;
        }

        // Сохраняем изменения в базе данных
        // await unitOfWork.SaveChangesAsync(cancellationToken);

        // Определяем сообщение об успешном сохранении в зависимости от времени
        var successMessage = now.IsMorningReportTime() ? MorningSuccessMessage : EveningSuccessMessage;

        // Отправляем сообщение об успешном сохранении отчёта
        await client.SendMessage(
            chatId: request.ChatId,
            text: successMessage,
            parseMode: ParseMode.Html,
            cancellationToken: CancellationToken.None
        );

        // Получаем список ID проверяющих
        var examiners = await unitOfWork
            .Query<User>()
            .Where(u => u.IsExaminer)
            .Select(u => u.Id)
            .ToListAsync(CancellationToken.None);

        // Параллельно отправляем уведомления каждому проверяющему
        await Parallel.ForEachAsync(examiners, _parallelOptions, async (userId, ct) =>
        {
            try
            {
                // Отправляем сообщение проверяющему.
                await client.SendMessage(
                    chatId: userId,
                    text: string.Format(ReportSubmissionMessage, request.User?.FullName ?? string.Empty, request.Report),
                    parseMode: ParseMode.Html,
                    replyMarkup: ExamKeyboard.ExamReportKeyboard(report.Id),
                    cancellationToken: ct
                );
            }
            catch (ApiRequestException ex) // Ловим только ошибки Telegram API
            {
                // Логируем ошибку, если не удалось отправить сообщение.
                logger.LogWarning(ex, "Failed to send report submission notification to user {UserId}.", userId);
            }
        });
    }

    /// <summary>
    /// Валидирует введённый отчёт.
    /// </summary>
    /// <param name="report">Введённый отчёт.</param>
    /// <returns>True, если отчёт корректен, иначе false.</returns>
    private static bool IsReportValid(string? report)
    {
        // Проверка, что отчёт не пустой и не состоит только из пробелов
        if (string.IsNullOrWhiteSpace(report)) return false;

        // Проверка, что длина отчёта не превышает 5000 символов
        return report.Length <= 5000;
    }
}