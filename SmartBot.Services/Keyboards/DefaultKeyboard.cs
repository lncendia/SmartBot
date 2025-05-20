using Telegram.Bot.Types.ReplyMarkups;

namespace SmartBot.Services.Keyboards;

/// <summary>
/// Статический класс для создания стандартных клавиатур бота
/// </summary>
public static class DefaultKeyboard
{
    /// <summary>
    /// Callback-данные для кнопки отмены действия
    /// </summary>
    public const string CancelCallbackData = "cancel";

    /// <summary>
    /// Префикс callback-данных для кнопки ответа на сообщение
    /// </summary>
    public const string AnswerCallbackData = "answer_";

    /// <summary>
    /// Callback-данные для отправки отчета на ручной анализ
    /// </summary>
    public const string SendForManualAnalysisCallbackData = "send_manual_analysis";
    
    /// <summary>
    /// Callback-данные для повторного анализа отчёта
    /// </summary>
    public const string RepeatAnalysisCallbackData = "repeat_analysis";

    /// <summary>
    /// Создаёт клавиатуру с одной кнопкой "Отмена"
    /// </summary>
    public static InlineKeyboardMarkup CancelKeyboard { get; } = new(
        InlineKeyboardButton.WithCallbackData(
            text: "❌ Отменить",
            callbackData: CancelCallbackData
        )
    );

    /// <summary>
    /// Клавиатура с опцией отправки отчёта на ручную модерацию
    /// </summary>
    /// <remarks>
    /// Используется в следующих случаях:
    /// - Система автоматического анализа недоступна
    /// - Требуется принудительная отправка на проверку модератору
    /// - Обнаружены проблемы при автоматической обработке отчёта
    /// </remarks>
    public static InlineKeyboardMarkup SendReportForManualAnalysisKeyboard { get; } = new(
        InlineKeyboardButton.WithCallbackData(
            text: "📝 Отправить на ручную проверку",
            callbackData: SendForManualAnalysisCallbackData
        )
    );


    /// <summary>
    /// Клавиатура с опцией повторного анализа отчета
    /// </summary>
    /// <remarks>
    /// Используется когда:
    /// - Система анализа не доступна
    /// </remarks>
    public static InlineKeyboardMarkup RepeatReportAnalysisKeyboard { get; } = new(
        new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "🔂 Повторить",
                    callbackData: RepeatAnalysisCallbackData
                )
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "📝 Отправить на ручную проверку",
                    callbackData: SendForManualAnalysisCallbackData
                )
            },
        }
    );

    /// <summary>
    /// Создаёт клавиатуру с кнопкой для ответа на сообщение
    /// </summary>
    /// <param name="reportId">Идентификатор отчёта</param>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="isEveningReport">Флаг вечернего отчёта</param>
    /// <returns>Inline-клавиатура с кнопкой ответа</returns>
    public static InlineKeyboardMarkup AnswerKeyboard(Guid reportId, long userId, bool isEveningReport)
    {
        // Создаём кнопку для ответа на сообщение
        var answerButton = InlineKeyboardButton.WithCallbackData(
            text: "✍️ Ответить",
            callbackData: $"{AnswerCallbackData}{reportId}_{userId}_{isEveningReport}"
        );

        // Возвращаем клавиатуру с одной кнопкой
        return new InlineKeyboardMarkup(answerButton);
    }
}