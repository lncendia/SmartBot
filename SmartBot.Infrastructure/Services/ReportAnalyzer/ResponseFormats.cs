using SmartBot.Infrastructure.Services.ReportAnalyzer.RequestModels;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer;

/// <summary>
/// Форматы запросов к OpenRouter.
/// </summary>
internal static class ResponseFormats
{
    /// <summary>
    /// Статическое поле, содержащее формат ответа, ожидаемый от API.
    /// </summary>
    public static readonly ResponseFormat AnalyzeReportResponseFormat = new()
    {
        Type = "json_schema",
        JsonSchema = new JsonSchema
        {
            Name = "report",
            Strict = true,
            Schema = new Schema
            {
                Type = "object",
                Properties = new Dictionary<string, Property>
                {
                    ["score"] = new()
                    {
                        Type = "number",
                        Description = "Оценка отчёта"
                    },
                    ["edit"] = new()
                    {
                        Type = "string",
                        Description = "Отредактированный отчёт"
                    }
                },
                Required = ["score", "edit"],
                AdditionalProperties = false
            }
        }
    };
    
        /// <summary>
    /// Формат ответа для генерации утренней мотивации
    /// </summary>
    public static readonly ResponseFormat MorningMotivationResponseFormat = new()
    {
        Type = "json_schema",
        JsonSchema = new JsonSchema
        {
            Name = "morning_motivation",
            Strict = true,
            Schema = new Schema
            {
                Type = "object",
                Properties = new Dictionary<string, Property>
                {
                    ["recommendations"] = new()
                    {
                        Type = "string",
                        Description = "Рекомендации по планированию дня"
                    },
                    ["motivation"] = new()
                    {
                        Type = "string",
                        Description = "Мотивирующий текст"
                    },
                    ["humor"] = new()
                    {
                        Type = "string",
                        Description = "Юмористическая заметка"
                    }
                },
                Required = ["recommendations", "motivation", "humor"],
                AdditionalProperties = false
            }
        }
    };

    /// <summary>
    /// Формат ответа для генерации вечерней оценки
    /// </summary>
    public static readonly ResponseFormat EveningPraiseResponseFormat = new()
    {
        Type = "json_schema",
        JsonSchema = new JsonSchema
        {
            Name = "evening_praise",
            Strict = true,
            Schema = new Schema
            {
                Type = "object",
                Properties = new Dictionary<string, Property>
                {
                    ["achievements"] = new()
                    {
                        Type = "string",
                        Description = "Признание достижений"
                    },
                    ["praise"] = new()
                    {
                        Type = "string",
                        Description = "Похвала за проделанную работу"
                    },
                    ["humor"] = new()
                    {
                        Type = "string",
                        Description = "Юмористическая заметка"
                    }
                },
                Required = ["achievements", "praise", "humor"],
                AdditionalProperties = false
            }
        }
    };

    /// <summary>
    /// Формат ответа для оценки эффективности
    /// </summary>
    public static readonly ResponseFormat ScorePointsResponseFormat = new()
    {
        Type = "json_schema",
        JsonSchema = new JsonSchema
        {
            Name = "score_points",
            Strict = true,
            Schema = new Schema
            {
                Type = "object",
                Properties = new Dictionary<string, Property>
                {
                    ["score"] = new()
                    {
                        Type = "number",
                        Description = "Оценка эффективности от 0 до 10"
                    }
                },
                Required = ["score"],
                AdditionalProperties = false
            }
        }
    };
}