using Microsoft.AspNetCore.Mvc;
using SmartBot.Abstractions.Interfaces;
using Telegram.Bot.Types;

namespace SmartBot.Controllers;

/// <summary>
/// Контроллер для обработки обновлений от Telegram
/// </summary>
[ApiController]
[Route("[controller]")]
public class BotController(IUpdateHandler updateHandler) : ControllerBase
{
    /// <summary>
    /// Обрабатывает обновление
    /// </summary>
    /// <param name="update">Обновление</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат операции</returns>
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update update, CancellationToken ct)
    {
        // Обрабатываем обновление
        await updateHandler.HandleAsync(update, ct);

        // Возвращаем успешный результат
        return Ok();
    }
}