using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = SmartBot.Abstractions.Models.Users.User;

namespace SmartBot.Services.Services;

/// <summary>
/// Обработчик обновлений (updates) для бота.
/// </summary>
public class UpdateHandler : IUpdateHandler
{
    /// <summary>
    /// Логгер для логирования событий и ошибок.
    /// </summary>
    private readonly ILogger<UpdateHandler> _logger;

    /// <summary>
    /// Отправитель команд.
    /// </summary>
    private readonly ISender _sender;

    /// <summary>
    /// Фабрика для создания команд на основе сообщений.
    /// </summary>
    private readonly IMessageCommandFactory _messageCommandFactory;

    /// <summary>
    /// Фабрика для создания команд на основе callback-запросов.
    /// </summary>
    private readonly ICallbackQueryCommandFactory _callbackQueryCommandFactory;

    /// <summary>
    /// Контекст работы с данными (Unit of Work).
    /// </summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Сервис синхронизации пользователей.
    /// </summary>
    private readonly IUserSynchronizationService _synchronizationService;

    /// <summary>
    /// Конструктор класса UpdateHandler.
    /// </summary>
    /// <param name="logger">Логгер для логирования событий и ошибок.</param>
    /// <param name="sender">Отправитель команд.</param>
    /// <param name="messageCommandFactory">Фабрика для создания команд на основе сообщений.</param>
    /// <param name="callbackQueryCommandFactory">Фабрика для создания команд на основе callback-запросов.</param>
    /// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
    /// <param name="synchronizationService">Сервис синхронизации пользователей.</param>
    public UpdateHandler(
        ILogger<UpdateHandler> logger,
        ISender sender,
        IMessageCommandFactory messageCommandFactory,
        ICallbackQueryCommandFactory callbackQueryCommandFactory,
        IUnitOfWork unitOfWork,
        IUserSynchronizationService synchronizationService)
    {
        _logger = logger;
        _sender = sender;
        _messageCommandFactory = messageCommandFactory;
        _callbackQueryCommandFactory = callbackQueryCommandFactory;
        _unitOfWork = unitOfWork;
        _synchronizationService = synchronizationService;
    }

    /// <summary>
    /// Обрабатывает входящее обновление (update).
    /// </summary>
    /// <param name="update">Обновление, которое нужно обработать.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task HandleAsync(Update update, CancellationToken cancellationToken)
    {
        // Определяем обработчик в зависимости от типа обновления
        var handler = update.Type switch
        {
            UpdateType.Message => BotOnMessageReceived(update.Message!, cancellationToken),
            UpdateType.CallbackQuery => BotOnCallbackQueryReceived(update.CallbackQuery!, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        try
        {
            // Выполняем обработчик
            await handler;
        }
        catch (Exception exception)
        {
            // Логируем ошибку
            _logger.LogError(exception, "Update id: {Id}", update.Id);

            // Перебрасываем исключение дальше
            throw;
        }
    }

    /// <summary>
    /// Обработчик для неизвестных типов обновлений.
    /// </summary>
    /// <param name="update">Обновление, которое нужно обработать.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Завершенная задача.</returns>
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private static Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        // Пропускаем обработку неизвестных обновлений
        return Task.CompletedTask;
    }

    /// <summary>
    /// Обрабатывает входящий callback-запрос.
    /// </summary>
    /// <param name="callbackQuery">Callback-запрос.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        // Получаем пользователя из базы данных
        var user = await _unitOfWork.Query<User>()
            .FirstOrDefaultAsync(u => u.Id == callbackQuery.From.Id, cancellationToken: cancellationToken);

        // Если пользователь не найден, прерываем обработку
        if (user == null) return;
        
        // Если пользователь заблокирован - не продолжаем
        if (user.Role == Role.Blocked) return;

        // Создаем команду на основе callback-запроса
        var command = _callbackQueryCommandFactory.GetCommand(user, callbackQuery);

        // Если команда не создана, прерываем обработку
        if (command == null) return;

        // Синхронизируем пользователя
        await _synchronizationService.SynchronizeAsync(callbackQuery.From.Id, cancellationToken);

        try
        {
            // Отправляем команду
            await _sender.Send(command, cancellationToken);
        }
        finally
        {
            // Освобождаем синхронизацию
            _synchronizationService.Release(callbackQuery.From.Id);
        }
    }

    /// <summary>
    /// Обрабатывает входящее сообщение.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        // Если отправитель сообщения неизвестен - не продолжаем
        if (message.From == null) return;

        // Синхронизируем пользователя
        await _synchronizationService.SynchronizeAsync(message.From.Id, cancellationToken);

        try
        {
            // Если отправитель сообщения известен, получаем пользователя из базы данных
            var user = await _unitOfWork.Query<User>()
                .FirstOrDefaultAsync(u => u.Id == message.From.Id, cancellationToken: cancellationToken);

            // Если пользователь заблокирован - не продолжаем
            if (user?.Role == Role.Blocked) return;

            // Создаем команду на основе сообщения
            var command = _messageCommandFactory.GetCommand(user, message);

            // Если команда не создана, прерываем обработку
            if (command == null) return;

            // Отправляем команду
            await _sender.Send(command, cancellationToken);
        }
        finally
        {
            // Освобождаем синхронизацию
            _synchronizationService.Release(message.From.Id);
        }
    }
}