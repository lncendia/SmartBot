namespace SmartBot.Middlewares;

using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

/// <summary>
/// Middleware для проверки секретного токена в заголовках запроса.
/// </summary>
/// <param name="next">Следующий middleware в конвейере.</param>
/// <param name="secretToken">Секретный токен для проверки.</param>
public class SecretTokenMiddleware(RequestDelegate next, string secretToken)
{
    /// <summary>
    /// Метод, который вызывается для каждого HTTP-запроса.
    /// </summary>
    /// <param name="context">Контекст HTTP-запроса.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Проверяем, содержится ли секретный токен в заголовках запроса
        if (!context.Request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var incomingToken))
        {
            // Если токен отсутствует, возвращаем статус 401 (Unauthorized)
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Секретный токен отсутствует в заголовках запроса.");
            return;
        }

        // Сравниваем полученный токен с ожидаемым
        if (incomingToken != secretToken)
        {
            // Если токен не совпадает, возвращаем статус 403 (Forbidden)
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Неверный секретный токен.");
            return;
        }
        
        // Если токен корректен, передаем запрос следующему middleware
        await next(context);
    }
}