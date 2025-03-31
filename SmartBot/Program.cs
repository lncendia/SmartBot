using SmartBot.Extensions;
using SmartBot.Infrastructure.DatabaseInitialization;

// Создание экземпляра объекта builder с использованием переданных аргументов
var builder = WebApplication.CreateBuilder(args);

// Регистрируем сервисы логгирования
builder.AddLoggingServices();

// Добавление служб для хранилища
builder.Services.AddStorageServices(builder.Configuration);

// Добавляем сервисы для работы бота
builder.Services.AddTelegramServices(builder.Configuration);

// Добавляем сервисы анализа отчётов
builder.Services.AddReportsServices(builder.Configuration);

// Добавляем сервисы рассылки уведомлений
builder.Services.AddNotificationServices();

// Добавление служб медиатора
builder.Services.AddMediatorServices();

// Регистрация контроллеров с поддержкой сериализации JSON
builder.Services.AddControllers();

// Создание приложения на основе настроек builder
await using var app = builder.Build();

// Создаем область для инициализации баз данных
using (var scope = app.Services.CreateScope())
{
    // Инициализация начальных данных в базу данных
    await DatabaseInitializer.InitAsync(scope.ServiceProvider);
}

// Добавляем мидлварь обработки ошибок
app.UseSecretToken();

// Маппим контроллеры
app.MapControllers();

// Запуск приложения
await app.RunAsync();