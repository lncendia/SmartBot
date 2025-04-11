namespace SmartBot.Abstractions.Attributes;

/// <summary>
/// Атрибут для пометки команд, которые должны обрабатываться асинхронно
/// </summary>
/// <remarks>
/// Команды, помеченные этим атрибутом, будут отправляться через IAsyncSender
/// для обработки в фоновом режиме через очередь команд.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class AsyncCommandAttribute : Attribute;