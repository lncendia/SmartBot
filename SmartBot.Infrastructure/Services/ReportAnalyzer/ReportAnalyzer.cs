using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Interfaces.ReportAnalyzer;
using SmartBot.Abstractions.Interfaces.ReportAnalyzer.DTOs;
using SmartBot.Infrastructure.Services.ReportAnalyzer.Configuration;
using SmartBot.Infrastructure.Services.ReportAnalyzer.RequestModels;
using SmartBot.Infrastructure.Services.ReportAnalyzer.ResponseModels;

namespace SmartBot.Infrastructure.Services.ReportAnalyzer;

/// <summary>
/// Класс, реализующий функциональность анализа отчётов с использованием API OpenRouter.
/// </summary>
/// <param name="configuration">Конфигурация анализатора, содержащая модель и подсказку.</param>
/// <param name="clientFactory">IHttpClientFactory для отправки HTTP-запросов.</param>
/// <param name="logger">Логгер.</param>
public class ReportAnalyzer(
    AnalyzerConfiguration configuration,
    IHttpClientFactory clientFactory,
    ILogger<ReportAnalyzer> logger) : IReportAnalyzer
{
    /// <summary>
    /// Имя HttpClient для фабрики.
    /// </summary>
    public const string HttpClientName = "openrouter";

    /// <summary>
    /// Базовый метод для выполнения запросов к API OpenRouter
    /// </summary>
    /// <typeparam name="TResponse">Тип модели ответа</typeparam>
    /// <param name="requestModel">Модель запроса</param>
    /// <param name="token">Токен отмены</param>
    /// <returns>Результат обработки ответа API</returns>
    private async Task<TResponse> ExecuteOpenRouterRequestAsync<TResponse>(
        RequestModel requestModel,
        CancellationToken token)
        where TResponse : class
    {
        // Сериализуем модель запроса в JSON
        var jsonContent = JsonSerializer.Serialize(requestModel);

        // Создаем HTTP-контент с JSON-данными
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Создаем HttpClient с помощью фабрики
        var client = clientFactory.CreateClient(HttpClientName);

        // Отправляем POST-запрос на API OpenRouter
        var response = await client.PostAsync("/api/v1/chat/completions", httpContent, token);

        // Проверяем успешность запроса (выбросит исключение при коде статуса не 2xx)
        response.EnsureSuccessStatusCode();

        // Читаем ответ от API в виде строки
        var responseJson = await response.Content.ReadAsStringAsync(token);

        try
        {
            // Находим индекс первого символа '{' - начало JSON-объекта
            var jsonStart = responseJson.IndexOf('{');

            // Находим индекс последнего символа '}' - конец JSON-объекта
            var jsonEnd = responseJson.LastIndexOf('}');

            // Инициализируем переменную для очищенного JSON
            // По умолчанию используем исходный текст, если не найдены границы
            var cleanJson = responseJson;

            // Проверяем что:
            // 1. Найден символ начала '{' (индекс >= 0)
            // 2. Найден символ конца '}' (индекс >= 0) 
            // 3. Конец находится после начала (jsonEnd > jsonStart)
            if (jsonStart >= 0 && jsonEnd >= 0 && jsonEnd > jsonStart)
            {
                // Вырезаем подстроку от начала до конца JSON-объекта включительно
                // Длина подстроки = (индекс_конца - индекс_начала + 1)
                cleanJson = responseJson.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }
            
            // Десериализуем JSON-ответ в промежуточный объект
            var openRouterResponse = JsonSerializer.Deserialize<OpenRouterResponse>(cleanJson);

            // Извлекаем содержимое ответа (message.content)
            var responseContent = openRouterResponse!.Choices.First().Message.Content;

            // Десериализуем содержимое ответа в целевую модель
            var aiResponse = JsonSerializer.Deserialize<TResponse>(responseContent);

            // Преобразуем ответ API в конечный результат
            return aiResponse!;
        }
        catch (JsonException ex)
        {
            // Логируем ошибку десериализации
            logger.LogWarning(ex, "Failed to process OpenRouter response: {Response}", responseJson.Trim());
            throw;
        }
    }

    /// <inheritdoc/>
    /// <summary>
    /// Анализирует отчет и возвращает структурированные результаты анализа
    /// </summary>
    public async Task<ReportAnalysisResult> AnalyzeReportAsync(string report, CancellationToken token)
    {
        // Создаем модель запроса для анализа отчета
        var requestModel = new RequestModel
        {
            Model = configuration.Model,
            Messages =
            [
                new Message { Role = "system", Content = configuration.ReportAnalysisPrompt },
                new Message { Role = "user", Content = report }
            ],
            ResponseFormat = ResponseFormats.AnalyzeReportResponseFormat
        };

        // Выполняем запрос к API и получаем сырой ответ
        var apiResponse = await ExecuteOpenRouterRequestAsync<ReportAnalysisResponse>(requestModel, token);

        // Преобразуем ответ API в доменный объект
        return new ReportAnalysisResult
        {
            Score = apiResponse.Score,
            Recommendations = apiResponse.Edit
        };
    }

    /// <inheritdoc/>
    /// <summary>
    /// Генерирует утреннюю мотивацию на основе планов из отчета
    /// </summary>
    public async Task<MorningMotivationResult> GenerateMorningMotivationAsync(string report, CancellationToken token)
    {
        // Создаем модель запроса для генерации мотивации
        var requestModel = new RequestModel
        {
            Model = configuration.Model,
            Messages =
            [
                new Message { Role = "system", Content = configuration.MorningMotivationPrompt },
                new Message { Role = "user", Content = report }
            ],
            ResponseFormat = ResponseFormats.MorningMotivationResponseFormat
        };

        // Выполняем запрос к API и получаем сырой ответ
        var apiResponse = await ExecuteOpenRouterRequestAsync<MorningMotivationResponse>(requestModel, token);

        // Преобразуем ответ API в доменный объект
        return new MorningMotivationResult
        {
            Recommendations = apiResponse.Recommendations,
            Motivation = apiResponse.Motivation,
            Humor = apiResponse.Humor
        };
    }

    /// <inheritdoc/>
    /// <summary>
    /// Генерирует вечернюю оценку выполненной работы
    /// </summary>
    public async Task<EveningPraiseResult> GenerateEveningPraiseAsync(string report, CancellationToken token)
    {
        // Создаем модель запроса для генерации оценки
        var requestModel = new RequestModel
        {
            Model = configuration.Model,
            Messages =
            [
                new Message { Role = "system", Content = configuration.EveningPraisePrompt },
                new Message { Role = "user", Content = report }
            ],
            ResponseFormat = ResponseFormats.EveningPraiseResponseFormat
        };

        // Выполняем запрос к API и получаем сырой ответ
        var apiResponse = await ExecuteOpenRouterRequestAsync<EveningPraiseResponse>(requestModel, token);

        // Преобразуем ответ API в доменный объект
        return new EveningPraiseResult
        {
            Achievements = apiResponse.Achievements,
            Praise = apiResponse.Praise,
            Humor = apiResponse.Humor
        };
    }

    /// <inheritdoc/>
    /// <summary>
    /// Вычисляет балльную оценку эффективности работы
    /// </summary>
    public async Task<double> GetScorePointsAsync(string report, CancellationToken token)
    {
        // Создаем модель запроса для оценки эффективности
        var requestModel = new RequestModel
        {
            Model = configuration.Model,
            Messages =
            [
                new Message { Role = "system", Content = configuration.ScorePointsPrompt },
                new Message { Role = "user", Content = report }
            ],
            ResponseFormat = ResponseFormats.ScorePointsResponseFormat
        };

        // Выполняем запрос к API и получаем сырой ответ
        var apiResponse = await ExecuteOpenRouterRequestAsync<ScorePointsResponse>(requestModel, token);

        // Возвращаем числовую оценку эффективности
        return apiResponse.Score;
    }
}