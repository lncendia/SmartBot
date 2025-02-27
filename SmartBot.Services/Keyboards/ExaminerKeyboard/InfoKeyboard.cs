using Telegram.Bot.Types.ReplyMarkups;

namespace SmartBot.Services.Keyboards.AdminKeyboard;

public static class InfoKeyboard
{
    public static InlineKeyboardMarkup Info(List<(string? Username, long Id)> ids)
    {
        var rows = new List<List<InlineKeyboardButton>>();
        ids.ForEach(key =>
        {
            rows.Add([
                InlineKeyboardButton.WithCallbackData(key.Item1 ?? $"Имя отсутствует ({key.Id})",
                    $"info_{key.Item2.ToString()}")
            ]);
        });
        return new InlineKeyboardMarkup(rows);
    }
}