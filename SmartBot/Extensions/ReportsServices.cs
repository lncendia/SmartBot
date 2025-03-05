using System.Net.Http.Headers;
using SmartBot.Abstractions.Interfaces.DataExporter;
using SmartBot.Abstractions.Interfaces.ReportAnalyzer;
using SmartBot.HostedServices;
using SmartBot.Infrastructure.Services.DataExporter;
using SmartBot.Infrastructure.Services.ReportAnalyzer;

namespace SmartBot.Extensions;

/// <summary>
/// Статический класс сервисов анализа отчетов.
/// </summary>
public static class ReportsServices
{
    /// <summary>
    /// Добавляет сервисы анализа отчетов.
    /// </summary>
    /// <param name="services">Коллекция служб, в которую будут добавлены новые сервисы.</param>
    /// <param name="configuration">Конфигурация приложения, содержащая параметры подключения и пути к хранилищам.</param>
    public static void AddReportsServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Получаем токен для OpenRouter
        var token = configuration.GetRequiredValue<string>("Openrouter:Token");

        // Создаем конфигурацию для анализатора
        var analyzerConfiguration = new AnalyzerConfiguration
        {
            // Получаем prompt для анализатора
            Prompt = configuration.GetRequiredValue<string>("Openrouter:Prompt"),

            // Получаем модель для анализатора
            Model = configuration.GetRequiredValue<string>("Openrouter:Model"),
        };

        // Получаем путь к файлу с учетными данными Google
        var googleCredentialsPath = configuration.GetRequiredValue<string>("Exporting:GoogleCredentialsPath");

        // Считываем содержимое файла с учетными данными Google
        var googleCredentials = File.ReadAllText(googleCredentialsPath);

        // Создаем конфигурацию для Google Sheets
        var googleSheetsConfiguration = new GoogleSheetsConfiguration
        {
            // Устанавливаем имя приложения
            ApplicationName = configuration.GetRequiredValue<string>("Exporting:ApplicationName"),

            // Устанавливаем идентификатор листа
            SheetId = configuration.GetRequiredValue<int>("Exporting:SheetId"),

            // Устанавливаем идентификатор таблицы
            SpreadsheetId = configuration.GetRequiredValue<string>("Exporting:SpreadsheetId"),

            // Устанавливаем учетные данные Google
            CredentialsJson = googleCredentials,
        };

        // Регистрируем конфигурацию Google Sheets как синглтон
        services.AddSingleton(googleSheetsConfiguration);

        // Добавляем конфигурацию для анализатора как синглтон
        services.AddSingleton(analyzerConfiguration);

        // Добавляем HTTP-клиент для анализатора отчетов
        services.AddHttpClient(ReportAnalyzer.HttpClientName, client =>
        {
            // Устанавливаем базовый адрес
            client.BaseAddress = new Uri("https://openrouter.ai");

            // Устанавливаем заголовки
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Устанавливаем тип принимаемого контента
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        // Добавляем сервис анализатора отчетов
        services.AddKeyedSingleton<IReportAnalyzer, ReportAnalyzer>("InnerReportAnalyzer");

        // Добавляем сервис анализатора отчетов с повторными попытками
        services.AddSingleton<IReportAnalyzer, ResilientAnalyzer>(sp =>
        {
            // Получаем сервис анализатора отчетов
            var reportAnalyzer = sp.GetRequiredKeyedService<IReportAnalyzer>("InnerReportAnalyzer");

            // Получаем логгер
            var logger = sp.GetRequiredService<ILogger<ResilientAnalyzer>>();

            // Возвращаем новый экземпляр ResilientAnalyzer
            return new ResilientAnalyzer(reportAnalyzer, logger);
        });

        // Добавляем сервис экспортирования данных в Google Sheets
        services.AddScoped<IDataExporter, GoogleSheetsExporter>();

        // Добавляем фоновый сервис экспорта отчётов
        services.AddHostedService<ExportingHostedService>();
        
        // Добавляем фоновый сервис отчистки старых отчётов
        services.AddHostedService<ClearingHostedService>();
    }
}