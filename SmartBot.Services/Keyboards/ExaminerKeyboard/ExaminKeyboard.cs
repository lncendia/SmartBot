using Telegram.Bot.Types.ReplyMarkups;

namespace SmartBot.Services.Keyboards.ExaminerKeyboard;

/// <summary>
/// Класс для создания клавиатуры для проверяющих.
/// </summary>
public static class ExamKeyboard
{
    /// <summary>
    /// Создаёт клавиатуру для проверяющих.
    /// </summary>
    /// <param name="reportId">Идентификатор отчёта.</param>
    /// <returns>Клавиатура для проверяющих.</returns>
    public static InlineKeyboardMarkup ExamReportKeyboard(Guid reportId)
    {
        // Создаём кнопку с callback-запросом
        var button = InlineKeyboardButton.WithCallbackData("Оставить комментарий 👀", $"exam_{reportId}");
       
        // Возвращаем клавиатуру с кнопкой
        return new InlineKeyboardMarkup(button);
    }
}