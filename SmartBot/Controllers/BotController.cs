using Microsoft.AspNetCore.Mvc;
using SmartBot.Abstractions.Interfaces;
using Telegram.Bot.Types;

namespace SmartBot.Controllers;

/// <summary>
/// Контроллер для обработки обновлений от Telegram
/// </summary>
[Route("[controller]")]
public class BotController(IUpdateHandler updateService) : ControllerBase
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
        await updateService.HandleAsync(update, ct);

        // Возвращаем успешный результат
        return Ok();
    }
}

// [Route("[controller]")]
// public class BotController(IServiceProvider provider) : ControllerBase
// {
//     [HttpPost]
//     public IActionResult Post([FromBody] Update update, CancellationToken ct)
//     {
//         var x = provider.CreateScope();
//         Do(x, update);
//         return Ok();
//     }

//     void Do(IServiceScope scope, Update update)
//     {
//         using (scope)
//         {
//             var updateService = scope.ServiceProvider.GetRequiredService<IUpdateHandler>();
//             updateService.HandleAsync(update, CancellationToken.None);
//         }
//     }
// }