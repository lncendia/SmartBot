using System.Net.Http.Headers;
using SmartBot.Abstractions.Interfaces.ReportAnalyzer;
using SmartBot.HostedServices;
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

        // Добавляем конфигурацию для анализатора
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

        // Добавляем фоновый сервис экспорта отчётов
        services.AddHostedService<ExportingHostedService>();
    }
}