using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Interfaces.DataExporter;

namespace SmartBot.Infrastructure.Services.DataExporter;

/// <summary>
/// Экспортер отчётов в Google Таблицу.
/// </summary>
public class GoogleSheetsExporter
{
    /// <summary>
    /// Конфигурация для экспорта отчётов в Google Таблицу.
    /// </summary>
    private readonly GoogleSheetsConfiguration _configuration;

    /// <summary>
    /// Сервис для работы с Google Sheets API.
    /// </summary>
    private readonly SheetsService _sheetsService;

    /// <summary>
    /// Логгер.
    /// </summary>
    private readonly ILogger<GoogleSheetsExporter> _logger;

    /// <summary>
    /// Конструктор класса.
    /// </summary>
    /// <param name="configuration">Конфигурация для экспорта отчётов в Google Таблицу.</param>
    /// <param name="logger">Логгер.</param>
    public GoogleSheetsExporter(GoogleSheetsConfiguration configuration, ILogger<GoogleSheetsExporter> logger)
    {
        // Инициализируем логгер
        _logger = logger;

        // Инициализируем конфигурацию
        _configuration = configuration;

        // Создаём сервис для работы с Google Sheets API
        var credential = GoogleCredential.FromJson(configuration.CredentialsJson);

        // Create a scoped credential for Android Publisher API
        credential = credential.CreateScoped(SheetsService.Scope.Spreadsheets);
        
        // Создание сервиса
        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            // Инициализируем сервис
            HttpClientInitializer = credential,

            // Указываем название приложения
            ApplicationName = _configuration.ApplicationName,
        });
    }
    

    /// <summary>
    /// Экспортирует массив отчетов в Google Таблицу.
    /// </summary>
    /// <param name="spreadsheetId">Идентификатор таблицы.</param>
    /// <param name="reports">Массив отчетов.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task ExportReportsAsync(string spreadsheetId, IReadOnlyList<ReportData> reports)
    {
        // Создаем список строк для записи в таблицу
        var values = new List<IList<object>>
        {
            // Добавляем заголовки столбцов
            new List<object>
            {
                "Name", "Position", "Morning Report", "Evening Report", "Date", "Comment"
            }
        };

        // Создаем список строк для записи в таблицу
        var reportsData = reports.Select(report => new List<object>
        {
            // Имя пользователя
            report.Name,

            // Должность пользователя
            report.Position,

            // Утренний отчёт
            report.MorningReport ?? string.Empty,

            // Вечерний отчёт
            report.EveningReport ?? string.Empty,

            // Дата отчёта
            report.Date.ToString("yyyy-MM-dd"),

            // Комментарий
            report.Comment ?? string.Empty
        });
        
        // Добавляем строки с данными отчётов
        values.AddRange(reportsData);

        // Создаем запрос на обновление таблицы
        var valueRange = new ValueRange {Values = values };

        // Выполняем запрос на обновление таблицы
        var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, _configuration.SheetRange);

        // Указываем, что вводимые значения являются необработанными
        updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

        // Выполняем запрос на обновление таблицы
        var response = await updateRequest.ExecuteAsync();

        // Логируем результат
        _logger.LogInformation("Data successfully exported to Google Sheets. Updated {UpdatedCells} cells.", response.UpdatedCells);
    }
}