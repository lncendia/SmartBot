using Telegram.Bot.Types.ReplyMarkups;

namespace SmartBot.Services.Keyboards;

/// <summary>
/// Класс для создания клавиатуры для администраторов.
/// </summary>
public static class AdminKeyboard
{
    /// <summary>
    /// Префикс для callback-данных кнопки "Оставить комментарий".
    /// </summary>
    public const string CommentReportCallbackData = "comment_";

    /// <summary>
    /// Префикс для callback-данных кнопки подтверждения отчёта.
    /// Формат: "approve_[reportId]_[isEveningReport]"
    /// </summary>
    public const string ApproveReportCallbackData = "approve_";

    /// <summary>
    /// Префикс для callback-данных кнопки отклонения отчёта.
    /// Формат: "reject_[reportId]_[isEveningReport]"
    /// </summary>
    public const string RejectReportCallbackData = "reject_";

    /// <summary>
    /// Callback-данные для кнопки "Назад".
    /// </summary>
    public const string GoBackCallbackData = "admin_goback";

    /// <summary>
    /// Callback-данные для кнопки удаления чата (с постфиксом ID чата).
    /// </summary>
    public const string DeleteChatCallbackData = "delete_chat_";

    /// <summary>
    /// Callback-данные для кнопки выбора чата (с постфиксом ID чата).
    /// </summary>
    public const string SelectChatCallbackData = "select_chat_";

    /// <summary>
    /// Callback-данные для команды изменения пользователя.
    /// </summary>
    public const string ChangeUserCallbackData = "change_user";
    
    /// <summary>
    /// Callback-данные для команды добавления рабочего чата.
    /// </summary>
    public const string AddWorkingChatCallbackData = "add_working_chat";

    /// <summary>
    /// Callback-данные для команды удаления рабочего чата.
    /// </summary>
    public const string RemoveWorkingChatCallbackData = "remove_working_chat";

    /// <summary>
    /// Callback-данные для команды установки рабочего чата пользователю.
    /// </summary>
    public const string SetWorkingChatCallbackData = "set_working_chat";

    /// <summary>
    /// Callback-данные для выбора обычного администратора.
    /// </summary>
    public const string AssignAdminPart = "assign-admin";

    /// <summary>
    /// Callback-данные для выбора теле-администратора.
    /// </summary>
    public const string AssignTeleAdminPart = "assign-teleadmin";
    
    /// <summary>
    /// Callback-данные для выбора обычного администратора.
    /// </summary>
    public const string DemoteAdminPart = "demote-admin";
    
    /// <summary>
    /// Часть callback-данных, идентифицирующая изменение имени пользователя.
    /// Используется для формирования полных callback-данных.
    /// </summary>
    public const string EditUserNamePart = "name";

    /// <summary>
    /// Часть callback-данных, идентифицирующая изменение должности пользователя.
    /// Используется для формирования полных callback-данных.
    /// </summary>
    public const string EditUserPositionPart = "position";
    
    /// <summary>
    /// Часть callback-данных, идентифицирующая изменение должности пользователя.
    /// Используется для формирования полных callback-данных.
    /// </summary>
    public const string BlockUserPositionPart = "block";

    /// <summary>
    /// Callback-данные для команды изменения пользователя.
    /// </summary>
    public const string EditUserCallbackData = "edit_user_";
    
    /// <summary>
    /// Полные callback-данные для выбора операции изменения имени пользователя.
    /// </summary>
    private const string EditUserNameCallbackData = $"{EditUserCallbackData}{EditUserNamePart}";

    /// <summary>
    /// Полные callback-данные для выбора операции изменения должности пользователя.
    /// </summary>
    private const string EditUserPositionCallbackData = $"{EditUserCallbackData}{EditUserPositionPart}";
    
    /// <summary>
    /// Callback-данные для команды назначения администратора.
    /// </summary>
    private const string AssignAdminCallbackData = $"{EditUserCallbackData}{AssignAdminPart}";
    
    /// <summary>
    /// Callback-данные для команды назначения администратора.
    /// </summary>
    private const string AssignTeleAdminCallbackData = $"{EditUserCallbackData}{AssignTeleAdminPart}";

    /// <summary>
    /// Callback-данные для команды разжалования администратора.
    /// </summary>
    private const string DemoteAdminCallbackData = $"{EditUserCallbackData}{DemoteAdminPart}";
    
    /// <summary>
    /// Callback-данные для команды разжалования администратора.
    /// </summary>
    private const string BlockUserCallbackData = $"{EditUserCallbackData}{BlockUserPositionPart}";

    /// <summary>
    /// Основная клавиатура администратора с действиями.
    /// </summary>
    public static InlineKeyboardMarkup MainKeyboard { get; } = new(
        new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "✏️ Изменить пользователя",
                    callbackData: ChangeUserCallbackData
                )
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "🆕 Добавить рабочий чат",
                    callbackData: AddWorkingChatCallbackData
                )
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "🗑️ Удалить рабочий чат",
                    callbackData: RemoveWorkingChatCallbackData
                )
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "💬 Назначить рабочий чат",
                    callbackData: SetWorkingChatCallbackData
                )
            }
        }
    );

    /// <summary>
    /// Создаёт клавиатуру для администраторов с кнопкой "Оставить комментарий".
    /// </summary>
    /// <param name="reportId">Идентификатор отчёта, к которому будет привязан комментарий.</param>
    /// <param name="isEveningReport">Флаг вечернего отчёта.</param>
    /// <returns>Клавиатура с кнопкой для оставления комментария.</returns>
    public static InlineKeyboardMarkup CommentReportKeyboard(Guid reportId, bool isEveningReport)
    {
        // Создаём кнопку с callback-запросом
        var button = InlineKeyboardButton.WithCallbackData(
            text: "Оставить комментарий 👀",
            callbackData: $"{CommentReportCallbackData}{reportId}_{isEveningReport}"
        );

        // Возвращаем клавиатуру с одной кнопкой
        return new InlineKeyboardMarkup(button);
    }

    /// <summary>
    /// Создаёт интерактивную клавиатуру для администраторов с действиями по отчёту
    /// </summary>
    /// <param name="reportId">Идентификатор отчёта для привязки действий</param>
    /// <param name="isEveningReport">Флаг, указывающий на тип отчёта (true - вечерний, false - утренний)</param>
    /// <param name="username">Имя пользователя (опционально)</param>
    /// <returns>Клавиатура с кнопками подтверждения и отклонения отчёта</returns>
    public static InlineKeyboardMarkup VerifyReportKeyboard(Guid reportId, bool isEveningReport, string? username)
    {
        var usernameSection = username != null
            ? $"_{username}"
            : string.Empty;

        var keyboard = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "✅ Подтвердить",
                    callbackData: $"{ApproveReportCallbackData}{reportId}_{isEveningReport}{usernameSection}"
                )
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "❌ Отклонить",
                    callbackData: $"{RejectReportCallbackData}{reportId}_{isEveningReport}"
                )
            }
        };

        // Возвращаем клавиатуру
        return new InlineKeyboardMarkup(keyboard);
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
    
    /// <summary>
    /// Клавиатура для выбора типа изменяемых данных пользователя.
    /// </summary>
    public static InlineKeyboardMarkup EditUserTypeKeyboard { get; } = new(
        new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "👨‍💼 Назначить администратором",
                    callbackData: AssignAdminCallbackData
                ),
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "👨‍💻 Назначить теле-администратором",
                    callbackData: AssignTeleAdminCallbackData
                ),
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "👨‍⚖️ Разжаловать",
                    callbackData: DemoteAdminCallbackData
                )
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "📝 Изменить имя",
                    callbackData: EditUserNameCallbackData
                ),
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "💼 Изменить должность",
                    callbackData: EditUserPositionCallbackData
                ),
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "⛔ Заблокировать",
                    callbackData: BlockUserCallbackData
                )
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "↩️ Назад",
                    callbackData: GoBackCallbackData
                )
            }
        });

    /// <summary>
    /// Клавиатура для выбора рабочего чата с запросом доступа.
    /// </summary>
    /// <remarks>
    /// Позволяет администратору выбрать чат из списка доступных.
    /// Параметры:
    /// - Запрашивает именно чат (не канал)
    /// - Требует права администратора в выбранном чате
    /// </remarks>
    public static ReplyKeyboardMarkup SelectChatKeyboard { get; } = new(
        new KeyboardButton("📋 Выберите рабочий чат")
        {
            RequestChat = new KeyboardButtonRequestChat
            {
                ChatIsChannel = false,
                RequestTitle = true,
                BotIsMember = true,
                RequestId = 0
            }
        }
    )
    {
        ResizeKeyboard = true
    };

    /// <summary>
    /// Клавиатура для выбора пользователя из чата.
    /// </summary>
    /// <remarks>
    /// Позволяет администратору выбрать одного или нескольких пользователей.
    /// Параметры:
    /// - Максимальное количество выбираемых пользователей: 1
    /// - Требует, чтобы пользователи были участниками текущего чата
    /// </remarks>
    public static ReplyKeyboardMarkup SelectUserKeyboard { get; } = new(
        new KeyboardButton("👤 Выберите пользователя")
        {
            RequestUsers = new KeyboardButtonRequestUsers(0)
            {
                MaxQuantity = 1,
                UserIsBot = false
            }
        }
    )
    {
        ResizeKeyboard = true
    };


    /// <summary>
    /// Создает кнопки для выбора чата.
    /// </summary>
    /// <param name="channels">Список чатов.</param>
    /// <returns>Список кнопок с номерами чатов.</returns>
    public static InlineKeyboardMarkup DeleteChatKeyboard((long id, string name)[] channels)
    {
        // Создаем кнопки для каждого чата
        var keys = channels.Select((channel, index) =>
        {
            // Генерируем эмоджи-номер для кнопки
            var emojiNumber = NumberToEmoji(index + 1); // Нумерация с 1

            return new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    text: $"{emojiNumber} {channel.name}",
                    callbackData: $"{DeleteChatCallbackData}{channel.id}"
                )
            };
        }).ToList();

        // Добавляем кнопку "Назад"
        keys.Add([
            InlineKeyboardButton.WithCallbackData(
                text: "↩️ Назад",
                callbackData: GoBackCallbackData
            )
        ]);

        // Возвращаем клавиатуру
        return new InlineKeyboardMarkup(keys);
    }

    /// <summary>
    /// Создает кнопки для выбора чата.
    /// </summary>
    /// <param name="channels">Список чатов.</param>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Список кнопок с номерами чатов.</returns>
    public static InlineKeyboardMarkup SelectWorkingChatKeyboard((long id, string name)[] channels, long? userId = null)
    {
        // Создаем кнопки для каждого чата
        var keys = channels.Select((channel, index) =>
        {
            // Генерируем эмоджи-номер для кнопки
            var emojiNumber = NumberToEmoji(index + 1); // Нумерация с 1

            // Формируем Callback-данные
            var callbackData = userId.HasValue
                ? $"{SelectChatCallbackData}{userId}_{channel.id}"
                : $"{SelectChatCallbackData}{channel.id}";

            return new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    text: $"{emojiNumber} {channel.name}",
                    callbackData: callbackData
                )
            };
        }).ToList();

        // Если необходимо добавить кнопку "Назад"
        if (!userId.HasValue)
        {
            // Добавляем кнопку "Назад"
            keys.Add([
                InlineKeyboardButton.WithCallbackData(
                    text: "↩️ Назад",
                    callbackData: GoBackCallbackData
                )
            ]);
        }

        // Возвращаем клавиатуру
        return new InlineKeyboardMarkup(keys);
    }

    /// <summary>
    /// Преобразует число в строку эмоджи-номеров.
    /// </summary>
    /// <param name="number">Число для преобразования.</param>
    /// <returns>Строка с эмоджи-номерами.</returns>
    private static string NumberToEmoji(int number)
    {
        // Словарь для преобразования цифр в эмоджи
        var digitToEmoji = new Dictionary<int, string>
        {
            { 0, "0️⃣" },
            { 1, "1️⃣" },
            { 2, "2️⃣" },
            { 3, "3️⃣" },
            { 4, "4️⃣" },
            { 5, "5️⃣" },
            { 6, "6️⃣" },
            { 7, "7️⃣" },
            { 8, "8️⃣" },
            { 9, "9️⃣" }
        };

        // Преобразуем число в строку и разбиваем на цифры
        var digits = number.ToString().Select(c => int.Parse(c.ToString()));

        // Преобразуем каждую цифру в эмоджи и объединяем в строку
        return string.Join("", digits.Select(d => digitToEmoji[d]));
    }
}