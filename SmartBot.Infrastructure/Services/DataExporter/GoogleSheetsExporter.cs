﻿using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;
using SmartBot.Abstractions.Extensions;
using SmartBot.Abstractions.Interfaces.DataExporter;

namespace SmartBot.Infrastructure.Services.DataExporter;

/// <summary>
/// Экспортер отчётов в Google Таблицу.
/// </summary>
public class GoogleSheetsExporter : IDataExporter
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


    /// <inheritdoc/>
    /// <summary>
    /// Экспортирует массив отчетов в Google Таблицу.
    /// </summary>
    public async Task ExportReportsAsync(IEnumerable<ReportData> reports, CancellationToken token)
    {
        // Получаем данные о таблицах из Google Sheets
        var sheets = await _sheetsService.Spreadsheets.Get(_configuration.SpreadsheetId).ExecuteAsync(token);

        // Находим название листа по его идентификатору
        var sheet = sheets.Sheets.FirstOrDefault(s => s.Properties.SheetId == _configuration.SheetId)?.Properties.Title;

        // Получаем текущие данные из таблицы, чтобы определить последнюю строку
        var existingData =
            await _sheetsService.Spreadsheets.Values.Get(_configuration.SpreadsheetId, $"{sheet}!A:A")
                .ExecuteAsync(token);

        // Определяем последнюю строку
        var lastRow = existingData.Values?.Count ?? 0;

        // Создаем список строк для записи в таблицу
        var values = reports.Select(report => new List<object>
        {
            // Имя пользователя
            report.Name ?? string.Empty,

            // Должность пользователя
            report.Position ?? string.Empty,

            // Утренний отчёт
            report.MorningReport?.Data ?? string.Empty,

            // Время сдачи отчёта
            GetReportElementTime(report.MorningReport),

            // Способ проверки отчёта
            report.MorningReport == null
                ? string.Empty
                : report.MorningReport.Approved
                    ? "Администратором"
                    : "Автоматически",

            // Вечерний отчёт
            report.EveningReport?.Data ?? string.Empty,

            // Время сдачи отчёта
            GetReportElementTime(report.EveningReport),

            // Способ проверки отчёта
            report.EveningReport == null
                ? string.Empty
                : report.EveningReport.Approved
                    ? "Администратором"
                    : "Автоматически",

            // Дата отчёта
            report.Date.ToString("dd.MM.yyyy"),

            // Комментарий
            report.Comment ?? string.Empty
        }).ToList<IList<object>>();

        // Если данных нет, добавляем заголовки
        if (lastRow == 0)
        {
            values.Insert(0, new List<object>
            {
                "Имя",
                "Должность",
                "Утренний отчёт",
                "Время сдачи",
                "Проверен",
                "Вечерний отчёт",
                "Время сдачи",
                "Проверен",
                "Дата",
                "Комментарий"
            });
        }

        // Определяем диапазон для добавления новых данных
        var appendRange = $"{sheet}!A{lastRow + 1}";

        // Создаем запрос на добавление данных
        var valueRange = new ValueRange { Values = values };

        // Выполняем запрос на добавление данных
        var appendRequest = _sheetsService.Spreadsheets.Values.Append(
            valueRange,
            _configuration.SpreadsheetId,
            appendRange
        );

        // Указываем, что вводимые значения являются необработанными
        appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;

        // Выполняем запрос на добавление данных
        var response = await appendRequest.ExecuteAsync(token);

        // Логируем результат
        _logger.LogInformation("Data successfully appended to Google Sheets. Updated {UpdatedCells} rows.",
            response.Updates?.UpdatedRows ?? 0);

        // Применяем форматирование для добавленных ячеек
        await ApplyFormatingAsync(lastRow, values.Count - (lastRow == 0 ? 1 : 0), token);

        // Логируем результат форматирования
        _logger.LogInformation("Formatting applied successfully.");
    }

    /// <summary>
    /// Применяет форматирование к таблице, включая настройку ширины колонок, высоты строк, выравнивание и рамки.
    /// </summary>
    /// <param name="lastRow">Индекс последней строки в таблице.</param>
    /// <param name="reportsCount">Количество отчетов, которые нужно отформатировать.</param>
    /// <param name="token">Токен отмены для асинхронной операции.</param>
    private async Task ApplyFormatingAsync(int lastRow, int reportsCount, CancellationToken token)
    {
        // Создаем список запросов для форматирования
        var requests = new List<Request>();

        // Проверяем, нужно ли форматировать заголовки (если lastRow == 0, значит таблица пустая)
        if (lastRow == 0)
        {
            // Создаем запрос для установки высоты строки заголовков
            var headerRowHeightRequest = new Request
            {
                // Указываем параметры для изменения высоты строки
                UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                {
                    // Указываем диапазон строк (первая строка)
                    Range = new DimensionRange
                    {
                        SheetId = _configuration.SheetId,
                        Dimension = "ROWS",
                        StartIndex = 0,
                        EndIndex = 1
                    },

                    // Устанавливаем высоту строки в 60 пикселей
                    Properties = new DimensionProperties
                    {
                        PixelSize = 60
                    },

                    // Указываем, что нужно обновить только высоту строки
                    Fields = "pixelSize"
                }
            };

            // Добавляем запрос в список
            requests.Add(headerRowHeightRequest);

            // Создаем список запросов для настройки ширины колонок
            var columnWidthRequests = new List<Request>
            {
                // Запрос для установки ширины колонки 0 (Name) и 1 (Position) в 250 пикселей
                new()
                {
                    UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = _configuration.SheetId,
                            Dimension = "COLUMNS",
                            StartIndex = 0,
                            EndIndex = 2
                        },
                        Properties = new DimensionProperties
                        {
                            PixelSize = 250
                        },
                        Fields = "pixelSize"
                    }
                },

                // Запрос для установки ширины колонок 2 (Morning Report) в 400 пикселей
                new()
                {
                    UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = _configuration.SheetId,
                            Dimension = "COLUMNS",
                            StartIndex = 2,
                            EndIndex = 3
                        },
                        Properties = new DimensionProperties
                        {
                            PixelSize = 400
                        },
                        Fields = "pixelSize"
                    }
                },
                // Запрос для установки ширины колонки 3 и 4 (Morning Report Overdue и Approved) в 150 пикселей
                new()
                {
                    UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = _configuration.SheetId,
                            Dimension = "COLUMNS",
                            StartIndex = 3,
                            EndIndex = 5
                        },
                        Properties = new DimensionProperties
                        {
                            PixelSize = 150
                        },
                        Fields = "pixelSize"
                    }
                },
                // Запрос для установки ширины колонок 5 (Evening Report) в 400 пикселей
                new()
                {
                    UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = _configuration.SheetId,
                            Dimension = "COLUMNS",
                            StartIndex = 5,
                            EndIndex = 6
                        },
                        Properties = new DimensionProperties
                        {
                            PixelSize = 400
                        },
                        Fields = "pixelSize"
                    }
                },
                // Запрос для установки ширины колонки 6 и 7 (Evening Report Overdue и Approved) в 150 пикселей
                new()
                {
                    UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = _configuration.SheetId,
                            Dimension = "COLUMNS",
                            StartIndex = 6,
                            EndIndex = 8
                        },
                        Properties = new DimensionProperties
                        {
                            PixelSize = 150
                        },
                        Fields = "pixelSize"
                    }
                },

                // Запрос для установки ширины колонки 8 (Date) в 150 пикселей
                new()
                {
                    UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = _configuration.SheetId,
                            Dimension = "COLUMNS",
                            StartIndex = 8,
                            EndIndex = 9
                        },
                        Properties = new DimensionProperties
                        {
                            PixelSize = 150
                        },
                        Fields = "pixelSize"
                    }
                },

                // Запрос для установки ширины колонки 9 (Comment) в 300 пикселей
                new()
                {
                    UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = _configuration.SheetId,
                            Dimension = "COLUMNS",
                            StartIndex = 9,
                            EndIndex = 10
                        },
                        Properties = new DimensionProperties
                        {
                            PixelSize = 300
                        },
                        Fields = "pixelSize"
                    }
                }
            };

            // Добавляем все запросы настройки ширины колонок в общий список
            requests.AddRange(columnWidthRequests);

            // Создаем запрос для форматирования ячеек заголовков
            var headerFormatRequest = new Request
            {
                // Указываем параметры для форматирования ячеек
                RepeatCell = new RepeatCellRequest
                {
                    // Указываем диапазон ячеек (первая строка, все колонки)
                    Range = new GridRange
                    {
                        SheetId = _configuration.SheetId,
                        StartRowIndex = 0,
                        EndRowIndex = 1,
                        StartColumnIndex = 0,
                        EndColumnIndex = 10
                    },

                    // Устанавливаем параметры форматирования
                    Cell = new CellData
                    {
                        UserEnteredFormat = new CellFormat
                        {
                            // Настройки текста: жирный, размер 12, белый цвет
                            TextFormat = new TextFormat
                            {
                                Bold = true,
                                FontSize = 12,
                                ForegroundColor = new Color { Red = 1, Green = 1, Blue = 1 }
                            },
                            // Выравнивание по центру по горизонтали и вертикали
                            HorizontalAlignment = "CENTER",
                            VerticalAlignment = "MIDDLE",

                            // Темно-серый фон
                            BackgroundColor = new Color
                            {
                                Red = 0.2f,
                                Green = 0.2f,
                                Blue = 0.2f
                            }
                        }
                    },

                    // Указываем, какие свойства форматирования нужно обновить
                    Fields = "userEnteredFormat(textFormat,horizontalAlignment,verticalAlignment,backgroundColor)"
                }
            };

            // Добавляем запрос форматирования заголовков в список
            requests.Add(headerFormatRequest);

            // Увеличиваем lastRow, так как заголовки были добавлены
            lastRow++;
        }

        // Создаем запрос для форматирования данных
        var dataFormatRequest = new Request
        {
            // Указываем параметры для форматирования ячеек данных
            RepeatCell = new RepeatCellRequest
            {
                // Указываем диапазон ячеек (строки с данными, все колонки)
                Range = new GridRange
                {
                    SheetId = _configuration.SheetId,
                    StartRowIndex = lastRow,
                    EndRowIndex = lastRow + reportsCount,
                    StartColumnIndex = 0,
                    EndColumnIndex = 10
                },

                // Устанавливаем параметры форматирования
                Cell = new CellData
                {
                    UserEnteredFormat = new CellFormat
                    {
                        // Настройки текста: размер 11
                        TextFormat = new TextFormat
                        {
                            FontSize = 11
                        },

                        // Выравнивание по левому краю по горизонтали и по нижнему краю по вертикали
                        HorizontalAlignment = "LEFT",
                        VerticalAlignment = "BOTTOM",

                        // Добавляем рамки вокруг ячеек
                        Borders = new Borders
                        {
                            Top = new Border { Style = "SOLID" },
                            Bottom = new Border { Style = "SOLID" },
                            Left = new Border { Style = "SOLID" },
                            Right = new Border { Style = "SOLID" }
                        }
                    }
                },

                // Указываем, какие свойства форматирования нужно обновить
                Fields = "userEnteredFormat(textFormat,horizontalAlignment,verticalAlignment,borders)"
            }
        };

        // Добавляем запрос форматирования данных в список запросов.
        requests.Add(dataFormatRequest);

        // Создаем запрос для установки переноса текста в колонке с индексом 1 - 10.
        var wrapTextRequestColumn = new Request
        {
            // Указываем параметры для обновления ячеек.
            RepeatCell = new RepeatCellRequest
            {
                // Указываем диапазон колонок (1 - 10).
                Range = new GridRange
                {
                    // ID листа, на котором будут применяться изменения.
                    SheetId = _configuration.SheetId,

                    // Начальная колонка (индекс 1).
                    StartColumnIndex = 1,

                    // Конечная колонка (индекс 3, не включая).
                    EndColumnIndex = 10,

                    // Начальная строка (индекс 1, пропускаем заголовок).
                    StartRowIndex = 1,

                    // Конечная строка (последняя строка с данными).
                    EndRowIndex = lastRow + reportsCount
                },

                // Устанавливаем свойства ячеек.
                Cell = new CellData
                {
                    // Формат ячейки, который будет применён.
                    UserEnteredFormat = new CellFormat
                    {
                        // Включаем перенос текста в ячейках.
                        WrapStrategy = "WRAP"
                    }
                },

                // Указываем, что нужно обновить только свойство WrapStrategy.
                Fields = "userEnteredFormat.wrapStrategy"
            }
        };

        // Добавляем запрос форматирования данных в список запросов.
        requests.Add(wrapTextRequestColumn);

        // Создаем запрос для установки высоты строк данных.
        var dataRowHeightRequest = new Request
        {
            // Указываем параметры для изменения высоты строк.
            UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
            {
                // Указываем диапазон строк (строки с данными).
                Range = new DimensionRange
                {
                    // ID листа, на котором будут применяться изменения.
                    SheetId = _configuration.SheetId,

                    // Указываем, что работаем со строками.
                    Dimension = "ROWS",

                    // Начальный индекс строки (первая строка с данными).
                    StartIndex = lastRow,

                    // Конечный индекс строки (последняя строка с данными).
                    EndIndex = lastRow + reportsCount
                },

                // Устанавливаем свойства для изменения высоты строк.
                Properties = new DimensionProperties
                {
                    // Устанавливаем высоту строки в 80 пикселей.
                    PixelSize = 80
                },

                // Указываем, что нужно обновить только высоту строки.
                Fields = "pixelSize"
            }
        };

        // Добавляем запрос настройки высоты строк в список
        requests.Add(dataRowHeightRequest);

        // Создаем запрос на выполнение всех операций форматирования
        var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
        {
            // Передаем список всех запросов
            Requests = requests
        };

        // Выполняем запрос на форматирование
        await _sheetsService.Spreadsheets.BatchUpdate(batchUpdateRequest, _configuration.SpreadsheetId)
            .ExecuteAsync(token);
    }

    /// <summary>
    /// Форматирует время отчёта и информацию о просрочке (если есть) в читаемый строковый формат
    /// </summary>
    /// <param name="element">Элемент отчёта (может быть null)</param>
    /// <returns>
    /// Строка в формате "HH:mm:ss", дополненная информацией о просрочке, если она есть.
    /// Возвращает пустую строку, если элемент отчёта отсутствует.
    /// </returns>
    private static string GetReportElementTime(ReportElement? element)
    {
        // Если элемент отчёта не передан - возвращаем пустую строку
        if (element == null) return string.Empty;

        // Форматируем время элемента в стандартный часовой формат
        var formatedText = new DateTime(element.TimeOfDay.Ticks).ToString("HH:mm:ss");

        // Если есть информация о просрочке - добавляем её к результату
        if (element.Overdue.HasValue)
        {
            // Добавляем информацию о просрочке с переводом строки
            // FormatTimeSpan() преобразует TimeSpan в читаемый формат (например "2 дня 5 часов")
            formatedText = formatedText + "\nПросрочен на " + element.Overdue.FormatTimeSpan();
        }

        return formatedText;
    }
}