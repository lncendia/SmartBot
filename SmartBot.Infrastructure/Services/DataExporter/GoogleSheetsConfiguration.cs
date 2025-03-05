namespace SmartBot.Infrastructure.Services.DataExporter;

/// <summary>
/// Конфигурация для экспорта отчётов в Google Таблицу.
/// </summary>
public class GoogleSheetsConfiguration
{
    /// <summary>
    /// Название приложения.
    /// </summary>
    public required string ApplicationName { get; init; }
    
    /// <summary>
    /// Идентификатор листа для записи данных в таблицу.
    /// </summary>
    public required int SheetId { get; init; }

    /// <summary>
    /// Данные для авторизации в Google.
    /// </summary>
    public required string CredentialsJson { get; init; }
    
    /// <summary>
    /// Идентификатор таблицы.
    /// </summary>
    public required string SpreadsheetId { get; init; }
} 