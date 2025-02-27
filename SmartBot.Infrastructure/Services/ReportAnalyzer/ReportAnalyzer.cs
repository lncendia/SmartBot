using System.Text;
using System.Text.Json;
using SmartBot.Abstractions.Interfaces.ReportAnalyzer;
using SmartBot.Infrastructure.Services.ReportAnalyzer.RequestModels;
using SmartBot.Infrastructure.Services.ReportAnalyzer.ResponseModels;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer;

/// <summary>
/// Класс, реализующий функциональность анализа отчётов с использованием API OpenRouter.
/// </summary>
/// <param name="configuration">Конфигурация анализатора, содержащая модель и подсказку.</param>
/// <param name="clientFactory">IHttpClientFactory для отправки HTTP-запросов.</param>
public class ReportAnalyzer(AnalyzerConfiguration configuration, IHttpClientFactory clientFactory) : IReportAnalyzer
{
    /// <summary>
    /// Имя HttpClient для фабрики.
    /// </summary>
    public const string HttpClientName = "openrouter";

    /// <summary>
    /// Статическое поле, содержащее формат ответа, ожидаемый от API.
    /// </summary>
    private static readonly ResponseFormat ResponseFormat = new()
    {
        // Тип формата ответа (JSON Schema)
        Type = "json_schema",

        // Схема JSON
        JsonSchema = new JsonSchema
        {
            // Имя схемы
            Name = "report",

            // Строгая проверка схемы
            Strict = true,

            // Схема документа
            Schema = new Schema
            {
                // Тип объекта в схеме
                Type = "object",

                // Поля
                Properties = new Properties
                {
                    // Поле оценки
                    Score = new Property
                    {
                        // Схема
                        Type = "number",

                        // Описание свойства
                        Description = "Оценка исходного отчёта от 1 до 10"
                    },

                    // Поле измененного отчёта
                    Edit = new Property
                    {
                        // Тип свойства "Edit" (строка)
                        Type = "string",

                        // Описание свойства
                        Description = "Отредактированный отчёт"
                    }
                },

                // Обязательные свойства
                Required = ["score", "edit"],

                // Запрет на дополнительные свойства
                AdditionalProperties = false
            }
        }
    };

    /// <summary>
    /// Асинхронный метод для анализа отчёта с использованием API OpenRouter.
    /// </summary>
    /// <param name="report">Текст отчёта для анализа.</param>
    /// <returns>Результат анализа, содержащий оценку и рекомендации.</returns>
    public async Task<ReportAnalyzeResult> AnalyzeAsync(string report, CancellationToken token)
    {
        // Создание модели запроса для отправки в API
        var requestModel = new RequestModel
        {
            // Модель, указанная в конфигурации
            Model = configuration.Model,

            // Сообщение
            Messages =
            [
                new Message
                {
                    // Роль сообщения (система)
                    Role = "system",

                    // Подсказка из конфигурации
                    Content = configuration.Prompt
                },
                new Message
                {
                    // Роль сообщения (пользователь)
                    Role = "user",

                    // Текст отчёта для анализа
                    Content = $"Обработай отчёт, рекомендации пронумеруй: {report}"
                }
            ],

            // Формат ответа
            ResponseFormat = ResponseFormat
        };

        // Сериализация модели запроса в JSON
        var jsonContent = JsonSerializer.Serialize(requestModel);

        // Создание HTTP-контента с JSON-данными
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Создаем HttpClient
        var client = clientFactory.CreateClient(HttpClientName);

        // Отправка POST-запроса на API OpenRouter
        var response = await client.PostAsync("/api/v1/chat/completions", httpContent, token);

        // Проверка успешности запроса (выбросит исключение, если статус не 2xx)
        response.EnsureSuccessStatusCode();

        // Чтение ответа от API в виде строки
        var responseJson = await response.Content.ReadAsStringAsync(token);

        // Десериализация JSON-ответа в объект OpenRouterResponse
        var openRouterResponse = JsonSerializer.Deserialize<OpenRouterResponse>(responseJson);

        // Извлечение содержимого ответа (message.content)
        var responseContent = openRouterResponse!.Choices.First().Message.Content;

        // Десериализация содержимого ответа в объект Report
        var aiResponse = JsonSerializer.Deserialize<Report>(responseContent);

        // Возврат результата анализа
        return new ReportAnalyzeResult
        {
            // Оценка отчёта
            Score = aiResponse!.Score,

            // Рекомендации по отчёту
            Recommendations = aiResponse.Edit
        };
    }
}