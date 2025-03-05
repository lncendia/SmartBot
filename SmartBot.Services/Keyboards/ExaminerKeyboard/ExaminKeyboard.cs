using Telegram.Bot.Types.ReplyMarkups;

namespace SmartBot.Services.Keyboards.ExaminerKeyboard;

/// <summary>
/// Класс для создания клавиатуры для проверяющих.
/// </summary>
public static class ExamKeyboard
{
    /// <summary>
    /// Префикс для callback-данных кнопки "Оставить комментарий".
    /// </summary>
    public const string ExamReportCallbackData = "exam_";

    /// <summary>
    /// Callback-данные для кнопки "Назад".
    /// </summary>
    public const string GoBackCallbackData = "goback";

    /// <summary>
    /// Создаёт клавиатуру для проверяющих с кнопкой "Оставить комментарий".
    /// </summary>
    /// <param name="reportId">Идентификатор отчёта, к которому будет привязан комментарий.</param>
    /// <returns>Клавиатура с кнопкой для оставления комментария.</returns>
    public static InlineKeyboardMarkup ExamReportKeyboard(Guid reportId)
    {
        // Создаём кнопку с callback-запросом
        var button = InlineKeyboardButton.WithCallbackData(
            text: "Оставить комментарий 👀",
            callbackData: $"{ExamReportCallbackData}{reportId}"
        );

        // Возвращаем клавиатуру с одной кнопкой
        return new InlineKeyboardMarkup(button);
    }

    /// <summary>
    /// Клавиатура с кнопкой "Назад".
    /// </summary>
    public static InlineKeyboardMarkup GoBackKeyboard { get; } = new(
        InlineKeyboardButton.WithCallbackData(
            text: "Назад ↩️",
            callbackData: GoBackCallbackData
        )
    );
}