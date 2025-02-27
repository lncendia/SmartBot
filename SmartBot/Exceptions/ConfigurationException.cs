namespace SmartBot.Exceptions;

/// <summary>
/// Исключение, которое вызывается если в конфигурации не найден путь до
/// </summary>
/// <param name="path">Путь</param>
public class ConfigurationException(string path) : Exception($"The configuration does not contain a path for {path}");