using System.Net.Http.Headers;
using SmartBot.Abstractions.Configuration;
using SmartBot.Abstractions.Interfaces.DataExporter;
using SmartBot.Abstractions.Interfaces.ReportAnalyzer;
using SmartBot.HostedServices;
using SmartBot.Infrastructure.Services.DataExporter;
using SmartBot.Infrastructure.Services.ReportAnalyzer;
using SmartBot.Infrastructure.Services.ReportAnalyzer.Configuration;

namespace SmartBot.Extensions;

/// <summary>
/// Статический класс для регистрации сервисов работы с отчетами
/// </summary>
public static class ReportsServices
{
    /// <summary>
    /// Добавляет сервисы для работы с отчетами в DI-контейнер
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <param name="configuration">Конфигурация приложения</param>
    public static void AddReportsServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Получаем API-токен для сервиса OpenRouter из конфигурации
        var token = configuration.GetRequiredValue<string>("Openrouter:Token");

        // Создаем конфигурацию для анализатора отчетов
        var analyzerConfiguration = new AnalyzerConfiguration
        {
            Model = configuration.GetRequiredValue<string>("Openrouter:Model"),
            ReportAnalysisPrompt = configuration.GetRequiredValue<string>("ReportAnalysis:ReportAnalysisPrompt"),
            MorningMotivationPrompt = configuration.GetRequiredValue<string>("ReportAnalysis:MorningMotivationPrompt"),
            EveningPraisePrompt = configuration.GetRequiredValue<string>("ReportAnalysis:EveningPraisePrompt"),
            ScorePointsPrompt = configuration.GetRequiredValue<string>("ReportAnalysis:ScorePointsPrompt")
        };

        // Создаем конфигурацию для анализа отчетов
        var analysisConfiguration = new ReportAnalysisConfiguration
        {
            // Получаем флаг включения анализа отчетов из конфигурации
            Enabled = configuration.GetRequiredValue<bool>("ReportAnalysis:Enabled"),
          
            // Получаем минимальный проходной балл для отчетов из конфигурации
            MinScore = configuration.GetRequiredValue<int>("ReportAnalysis:MinScore"),
        };

        // Получаем путь к файлу с учетными данными Google из конфигурации
        var googleCredentialsPath = configuration.GetRequiredValue<string>("Exporting:GoogleCredentialsPath");

        // Читаем содержимое файла с учетными данными Google
        var googleCredentials = File.ReadAllText(googleCredentialsPath);

        // Создаем конфигурацию для работы с Google Sheets
        var googleSheetsConfiguration = new GoogleSheetsConfiguration
        {
            // Устанавливаем название приложения из конфигурации
            ApplicationName = configuration.GetRequiredValue<string>("Exporting:ApplicationName"),

            // Устанавливаем ID листа из конфигурации
            SheetId = configuration.GetRequiredValue<int>("Exporting:SheetId"),

            // Устанавливаем ID таблицы из конфигурации
            SpreadsheetId = configuration.GetRequiredValue<string>("Exporting:SpreadsheetId"),

            // Устанавливаем учетные данные Google
            CredentialsJson = googleCredentials,
        };

        // Регистрируем конфигурацию Google Sheets как singleton
        services.AddSingleton(googleSheetsConfiguration);

        // Регистрируем конфигурацию анализатора как singleton
        services.AddSingleton(analyzerConfiguration);
        
        // Регистрируем конфигурацию анализа отчетов как singleton
        services.AddSingleton(analysisConfiguration);

        // Добавляем HTTP-клиент для работы с API OpenRouter
        services.AddHttpClient(ReportAnalyzer.HttpClientName, client =>
        {
            // Устанавливаем базовый адрес API
            client.BaseAddress = new Uri("https://openrouter.ai");

            // Устанавливаем заголовок авторизации с токеном
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Устанавливаем заголовок Accept для JSON
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        // Регистрируем основной сервис анализатора отчетов с ключом
        services.AddKeyedSingleton<IReportAnalyzer, ReportAnalyzer>("InnerReportAnalyzer");

        // Регистрируем оберточный сервис анализатора с обработкой ошибок
        services.AddSingleton<IReportAnalyzer, ResilientAnalyzer>(sp =>
        {
            // Получаем основной сервис анализатора по ключу
            var reportAnalyzer = sp.GetRequiredKeyedService<IReportAnalyzer>("InnerReportAnalyzer");

            // Получаем логгер из DI-контейнера
            var logger = sp.GetRequiredService<ILogger<ResilientAnalyzer>>();

            // Создаем и возвращаем оберточный сервис
            return new ResilientAnalyzer(reportAnalyzer, logger);
        });

        // Регистрируем сервис экспорта данных в Google Sheets
        services.AddScoped<IDataExporter, GoogleSheetsExporter>();

        // Регистрируем фоновый сервис для экспорта отчетов
        services.AddHostedService<ExportingHostedService>();
        
        // Регистрируем фоновый сервис для очистки старых отчетов
        services.AddHostedService<ClearingHostedService>();
        
        // Регистрируем фоновый сервис для автоматического принятия отчётов по истечении времени ожидания
        services.AddHostedService<ReportApprovalHostedService>();
    }
}