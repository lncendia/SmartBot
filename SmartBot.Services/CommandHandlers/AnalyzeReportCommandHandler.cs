﻿using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Commands;
using SmartBot.Abstractions.Configuration;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Extensions;
using SmartBot.Abstractions.Interfaces;
using SmartBot.Abstractions.Interfaces.ReportAnalyzer;
using SmartBot.Abstractions.Models.Reports;
using SmartBot.Services.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = SmartBot.Abstractions.Models.Users.User;

namespace SmartBot.Services.CommandHandlers;

/// <summary>
/// Обработчик команды для анализа и сохранения отчёта.
/// </summary>
/// <param name="client">Клиент для взаимодействия с Telegram API.</param>
/// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
/// <param name="reportAnalyzer">Анализатор отчётов.</param>
/// <param name="dateTimeProvider">Провайдер для работы с текущим временем.</param>
/// <param name="logger">Логгер.</param>
/// <param name="options">Настройки параллелизма для рассылки сообщений.</param>
/// <param name="analyzerConfiguration">Конфигурация анализатора отчётов.</param>
/// <param name="synchronizationService">Сервис синхронизации пользователей.</param>
public class AnalyzeReportCommandHandler(
    ITelegramBotClient client,
    IUnitOfWork unitOfWork,
    IReportAnalyzer reportAnalyzer,
    IDateTimeProvider dateTimeProvider,
    ParallelOptions options,
    ReportAnalysisConfiguration analyzerConfiguration,
    IUserSynchronizationService synchronizationService,
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
        "Пожалуйста, исправьте отчёт на основе рекомендаций.\n\n" +
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

    // Сообщение, информирующее о временных ограничениях для отправки отчётов.
    // Уведомляет пользователя, что в текущее время отправка отчётов невозможна.
    // Также указывает, что пользователь получит уведомление, когда наступит время для отправки отчёта.
    private const string ReportTimeRestrictionMessage =
        "<b>Сейчас не время для отправки отчёта. ⏰</b>\n\n" +
        "Утренние отчёты принимаются с <b>9:00 до 10:00</b> по МСК, " +
        "а вечерние — с <b>18:00 до 19:00</b> по МСК. " +
        "Я отправлю вам уведомление, когда наступит время для отправки отчёта. 🛎";

    /// <summary>
    /// Сообщение для администратора о новом отчёте пользователя.
    /// Содержит имя пользователя и текст отчёта, а также приглашение оставить комментарий.
    /// </summary>
    private const string ReportSubmissionMessage =
        "📄 <b>Новый отчёт от пользователя</b> <i>{0}</i>\n" +
        "🧑‍🏭 <b>Должность:</b> <i>{1}</i>\n\n" +
        "👇 <b>Текст отчёта:</b>\n" +
        "<blockquote>{2}</blockquote>\n\n" +
        "📝 <i>Нажмите на кнопку, если хотите указать замечания или рекомендации для улучшения.</i>";

    /// <summary>
    /// Шаблон сообщения о просрочке утреннего отчёта.
    /// </summary>
    private const string MorningOverdueMessage =
        "⚠️ Вы просрочили утренний отчёт на {0}. Постарайтесь не задерживать отчёты в будущем!";

    /// <summary>
    /// Шаблон сообщения о просрочке вечернего отчёта.
    /// </summary>
    private const string EveningOverdueMessage =
        "⚠️ Вы просрочили вечерний отчёт на {0}. Постарайтесь не задерживать отчёты в будущем!";

    /// <summary>
    /// Шаблон сообщения о необходимости сдать вечерний отчёт.
    /// </summary>
    private const string EveningReportDueMessage =
        "🌇 <b>Внимание! Сейчас время для сдачи вечернего отчёта.</b>\n\n" +
        "Пожалуйста, отправьте ваш <b>вечерний отчёт</b> как можно скорее. " +
        "Это важно для подведения итогов дня и планирования завтрашних задач.\n\n" +
        "📝 <i>Не забудьте указать ключевые результаты и планы на завтра.</i>";

    /// <summary>
    /// Обрабатывает команду анализа отчёта.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде анализа отчёта.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task Handle(AnalyzeReportCommand request, CancellationToken cancellationToken)
    {
        // Синхронизируем пользователя
        await synchronizationService.SynchronizeAsync(request.TelegramUserId, cancellationToken);

        try
        {
            // Обрабатываем команду
            await ProcessCommandAsync(request, cancellationToken);
        }
        finally
        {
            // Освобождаем синхронизацию
            synchronizationService.Release(request.TelegramUserId);
        }
    }

    /// <summary>
    /// Обрабатывает команду анализа отчёта.
    /// </summary>
    /// <param name="request">Запрос, содержащий данные о команде анализа отчёта.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task ProcessCommandAsync(AnalyzeReportCommand request, CancellationToken cancellationToken)
    {
        // Валидируем введённый отчёт
        if (!IsReportValid(request.Report))
        {
            // Если отчёт пустой, отправляем сообщение об ошибке
            await client.SendMessage(
                replyParameters: new ReplyParameters
                {
                    MessageId = request.MessageId
                },
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
        if (!now.IsWorkingPeriod())
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

        // Записываем введенный отчёт пользователя в состояние, чтобы в дальнейшем можно было отправить его без проверки
        request.User!.CurrentReport = request.Report;

        // Получаем отчёт из базы данных за текущую дату для указанного пользователя
        var report = await unitOfWork.Query<Report>()
            .Where(r => r.UserId == request.TelegramUserId)
            .Where(r => r.Date.Date == now.Date)
            .FirstOrDefaultAsync(cancellationToken);

        // Если вечерний отчёт уже сдан - ничего не делаем
        if (report?.EveningReport != null)
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

        // Если утренний отчёт уже сдан и еще не время для сдачи вечернего отчёта
        if (report?.MorningReport != null && !now.IsEveningPeriod())
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

        // Если анализатор отчётов включен - анализируем отчёт
        if (analyzerConfiguration.Enabled)
        {
            // Объявляем переменную для хранения результата анализа отчёта
            ReportAnalyzeResult analysisResult;

            try
            {
                // Выполняем асинхронный анализ отчёта с использованием анализатора
                var task = reportAnalyzer.AnalyzeAsync(request.Report!, cancellationToken);

                // Пока задача не завершена, отправляем статус "печатает" в чат
                while (!task.IsCompleted)
                {
                    await client.SendChatAction(request.ChatId, ChatAction.Typing,
                        cancellationToken: cancellationToken);

                    // Ждём 2 секунды перед следующей проверкой
                    await Task.Delay(2000, cancellationToken);
                }

                // Получаем результат анализа
                analysisResult = task.Result;
            }
            catch
            {
                // Сохраняем изменения в базе данных.
                await unitOfWork.SaveChangesAsync(cancellationToken);

                // Если произошла ошибка при анализе отчёта, отправляем сообщение об ошибке в чат
                await client.SendMessage(
                    replyParameters: new ReplyParameters
                    {
                        MessageId = request.MessageId
                    },
                    chatId: request.ChatId,
                    text: AnalyzerUnavailableMessage,
                    replyMarkup: DefaultKeyboard.RepeatReportAnalysisKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );

                // Не продолжаем
                return;
            }

            // Если отчёт некорректен (оценка меньше MinScore), отправляем рекомендации
            if (analysisResult.Score < analyzerConfiguration.MinScore)
            {
                // Сохраняем изменения в базе данных.
                await unitOfWork.SaveChangesAsync(cancellationToken);

                // Отправляем сообщение с рекомендациями по исправлению отчёта
                await client.SendMessage(
                    replyParameters: new ReplyParameters
                    {
                        MessageId = request.MessageId
                    },
                    chatId: request.ChatId,
                    text: string.Format(ErrorMessage, analysisResult.Score, analysisResult.Recommendations),
                    replyMarkup: DefaultKeyboard.SendReportWithoutAnalysisKeyboard,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );

                // Завершаем выполнение метода
                return;
            }
        }

        // Получаем текущее время после анализа данных.
        var timeAfterAnalyze = dateTimeProvider.Now;

        // Если отчёт за текущую дату отсутствует, создаём новый.
        if (report == null)
        {
            // Создаём новый отчёт с текущей датой и ID пользователя.
            report = new Report
            {
                // Указываем ID пользователя, которому принадлежит отчёт.
                UserId = request.User!.Id,

                // Устанавливаем текущую дату для отчёта.
                Date = timeAfterAnalyze,

                // Заполняем данные утреннего отчёта.
                MorningReport = new UserReport
                {
                    // Данные отчёта, переданные в запросе.
                    Data = request.Report!,

                    // Проверяем, просрочен ли утренний отчёт.
                    Overdue = timeAfterAnalyze.MorningReportOverdue()
                }
            };

            // Добавляем отчёт в базу данных.
            await unitOfWork.AddAsync(report, cancellationToken);
        }
        else
        {
            // Если отчёт за текущую дату уже существует, обновляем вечерний отчёт.
            report.EveningReport = new UserReport
            {
                // Данные отчёта, переданные в запросе.
                Data = request.Report!,

                // Проверяем, просрочен ли вечерний отчёт.
                Overdue = timeAfterAnalyze.EveningReportOverdue()
            };
        }

        // Если отчёт прошел проверку - обнуляем состояние текущего введенного отчёта
        request.User!.CurrentReport = null;

        // Сохраняем изменения в базе данных.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Определяем сообщение об успешном сохранении в зависимости от времени.
        if (report.EveningReport == null)
        {
            // Отправляем сообщение об успешном сохранении утреннего отчёта.
            await client.SendMessage(
                replyParameters: new ReplyParameters
                {
                    MessageId = request.MessageId
                },
                chatId: request.ChatId,
                text: MorningSuccessMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );

            // Если утренний отчёт был просрочен, отправляем дополнительное уведомление.
            if (report.MorningReport.Overdue.HasValue)
            {
                // Формируем сообщение о просрочке.
                var overdueMessage =
                    string.Format(MorningOverdueMessage, report.MorningReport.Overdue.FormatTimeSpan());

                // Отправляем сообщение о просрочке.
                await client.SendMessage(
                    chatId: request.ChatId,
                    text: overdueMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: CancellationToken.None
                );
            }

            // Если сейчас время для сдачи вечернего отчёта
            if (timeAfterAnalyze.IsEveningPeriod())
            {
                // Отправляем сообщение о необходимости сдать вечерний отчёт.
                await client.SendMessage(
                    chatId: request.ChatId,
                    text: EveningReportDueMessage, // Используем константу с текстом сообщения
                    parseMode: ParseMode.Html,
                    cancellationToken: CancellationToken.None
                );
            }
        }
        else
        {
            // Отправляем сообщение об успешном сохранении вечернего отчёта.
            await client.SendMessage(
                replyParameters: new ReplyParameters
                {
                    MessageId = request.MessageId
                },
                chatId: request.ChatId,
                text: EveningSuccessMessage,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );

            // Если вечерний отчёт был просрочен, отправляем дополнительное уведомление.
            if (report.EveningReport.Overdue.HasValue)
            {
                // Формируем сообщение о просрочке.
                var overdueMessage =
                    string.Format(EveningOverdueMessage, report.EveningReport.Overdue.FormatTimeSpan());

                // Отправляем сообщение о просрочке.
                await client.SendMessage(
                    chatId: request.ChatId,
                    text: overdueMessage,
                    parseMode: ParseMode.Html,
                    cancellationToken: CancellationToken.None
                );
            }
        }

        // Получаем список ID администраторов
        var admins = await unitOfWork
            .Query<User>()
            .Where(u => u.Role == Role.Admin || u.Role == Role.TeleAdmin)
            .Select(u => u.Id)
            .ToListAsync(CancellationToken.None);

        // Создаем список из чатов, которые необходимо уведомить о новом отчёте
        var chatsToNotify = admins
            .Where(a => a != request.User.Id)
            .Select(a => new ValueTuple<long, int?>(a, null))
            .ToList();

        // Если у пользователя установлен рабочий чат - добавляем его в список чатов рассылки
        if (request.User!.WorkingChat != null)
        {
            // Сохраняем рабочий чат в переменную
            var workingChat = request.User.WorkingChat;

            // Создаем новый кортеж
            var tuple = new ValueTuple<long, int?>(workingChat.Id, workingChat.MessageThreadId);

            // Добавляем кортеж в список
            chatsToNotify.Add(tuple);
        }

        // Параллельно отправляем уведомления каждому администратору
        await Parallel.ForEachAsync(chatsToNotify, options, async (chat, ct) =>
        {
            try
            {
                // Отправляем сообщение администратору.
                await client.SendMessage(
                    chatId: chat.Item1,
                    messageThreadId: chat.Item2,
                    text: string.Format(
                        ReportSubmissionMessage,
                        request.User?.FullName,
                        request.User?.Position,
                        request.Report),
                    parseMode: ParseMode.Html,
                    replyMarkup: AdminKeyboard.ExamReportKeyboard(report.Id, report.EveningReport != null),
                    cancellationToken: ct
                );
            }
            catch (ApiRequestException ex) // Ловим только ошибки Telegram API
            {
                // Логируем ошибку, если не удалось отправить сообщение.
                logger.LogWarning(ex, "Failed to send report submission notification to chat {ChatId}.", chat.Item1);
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