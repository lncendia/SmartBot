using MediatR;

namespace SmartBot.Abstractions.Interfaces.Utils;

/// <summary>
/// Интерфейс асинхронного отправителя команд
/// </summary>
/// <remarks>
/// Реализации этого интерфейса должны обеспечивать постановку команд в очередь
/// для последующей фоновой обработки, что позволяет избежать таймаутов при
/// выполнении длительных операций.
/// </remarks>
public interface IAsyncSender
{
    /// <summary>
    /// Добавляет запрос в очередь на обработку
    /// </summary>
    /// <param name="request">Запрос для обработки</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Задача, представляющая асинхронную операцию постановки в очередь</returns>
    Task Send(IRequest request, CancellationToken ct = default);
}