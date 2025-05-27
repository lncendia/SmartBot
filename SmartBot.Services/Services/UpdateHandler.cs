using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Attributes;
using SmartBot.Abstractions.Enums;
using SmartBot.Abstractions.Interfaces.ComandFactories;
using SmartBot.Abstractions.Interfaces.Storage;
using SmartBot.Abstractions.Interfaces.UpdateHandler;
using SmartBot.Abstractions.Interfaces.Utils;
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
    /// Базовый отправитель команд для синхронной обработки
    /// </summary>
    private readonly ISender _sender;
    
    /// <summary>
    /// Асинхронный отправитель команд для фоновой обработки
    /// </summary>
    private readonly IAsyncSender _asyncSender;

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
    /// Конструктор класса UpdateHandler.
    /// </summary>
    /// <param name="logger">Логгер для логирования событий и ошибок.</param>
    /// <param name="sender">Отправитель команд.</param>
    /// <param name="messageCommandFactory">Фабрика для создания команд на основе сообщений.</param>
    /// <param name="callbackQueryCommandFactory">Фабрика для создания команд на основе callback-запросов.</param>
    /// <param name="unitOfWork">Контекст работы с данными (Unit of Work).</param>
    /// <param name="asyncSender">Асинхронный отправитель для фоновой обработки</param>
    public UpdateHandler(
        ILogger<UpdateHandler> logger,
        ISender sender,
        IMessageCommandFactory messageCommandFactory,
        ICallbackQueryCommandFactory callbackQueryCommandFactory,
        IUnitOfWork unitOfWork, 
        IAsyncSender asyncSender)
    {
        _logger = logger;
        _sender = sender;
        _messageCommandFactory = messageCommandFactory;
        _callbackQueryCommandFactory = callbackQueryCommandFactory;
        _unitOfWork = unitOfWork;
        _asyncSender = asyncSender;
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
            .Where(u => u.Role != Role.Blocked)
            .FirstOrDefaultAsync(u => u.Id == callbackQuery.From.Id, cancellationToken: cancellationToken);

        // Если пользователь не найден, прерываем обработку
        if (user == null) return;

        // Создаем команду на основе callback-запроса
        var command = _callbackQueryCommandFactory.GetCommand(user, callbackQuery);

        // Если команда не создана, прерываем обработку
        if (command == null) return;

        // Отправляем команду
        await SendCommandAsync(command, cancellationToken);
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
        await SendCommandAsync(command, cancellationToken);
    }


    /// <summary>
    /// Отправляет команду на обработку, определяя синхронный или асинхронный способ выполнения
    /// на основе наличия атрибута <see cref="AsyncCommandAttribute"/> у запроса.
    /// </summary>
    /// <param name="request">Команда или запрос для обработки</param>
    /// <param name="token">Токен отмены операции</param>
    /// <returns>Задача, представляющая асинхронную операцию отправки</returns>
    /// <remarks>
    /// Если запрос помечен атрибутом <see cref="AsyncCommandAttribute"/>, он будет обработан
    /// асинхронным отправителем через очередь. Обычные запросы обрабатываются синхронно.
    /// </remarks>
    private Task SendCommandAsync(IRequest request, CancellationToken token)
    {
        // Получаем все атрибуты типа запроса
        var attributes = request.GetType().GetCustomAttributes(false);

        // Проверяем наличие атрибута AsyncCommandAttribute
        return attributes.Any(a => a.GetType() == typeof(AsyncCommandAttribute)) 
            ? _asyncSender.Send(request, token)  // Асинхронная обработка через очередь
            : _sender.Send(request, token);     // Синхронная обработка
    }
}