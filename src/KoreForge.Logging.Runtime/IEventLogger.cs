using System;
using Microsoft.Extensions.Logging;

namespace KoreForge.Logging;

/// <summary>
/// Strongly typed logging surface produced for each generated enumeration value.
/// </summary>
public interface IEventLogger
{
    /// <summary>
    /// Determines whether the backing logger enables the supplied level.
    /// </summary>
    /// <param name="level">Log level to query.</param>
    bool IsEnabled(LogLevel level);

    /// <summary>
    /// Logs a trace message using the event metadata.
    /// </summary>
    /// <param name="message">Template or message text.</param>
    /// <param name="args">Optional structured logging arguments.</param>
    void LogTrace(string message, params object?[] args);
    /// <summary>
    /// Logs a trace message that includes the supplied exception.
    /// </summary>
    /// <param name="exception">Exception to attach.</param>
    /// <param name="message">Template or message text.</param>
    /// <param name="args">Optional structured logging arguments.</param>
    void LogTrace(Exception exception, string message, params object?[] args);

    /// <summary>
    /// Logs a debug message using the event metadata.
    /// </summary>
    /// <param name="message">Template or message text.</param>
    /// <param name="args">Optional structured logging arguments.</param>
    void LogDebug(string message, params object?[] args);
    /// <summary>
    /// Logs a debug message that includes the supplied exception.
    /// </summary>
    /// <param name="exception">Exception to attach.</param>
    /// <param name="message">Template or message text.</param>
    /// <param name="args">Optional structured logging arguments.</param>
    void LogDebug(Exception exception, string message, params object?[] args);

    /// <summary>
    /// Logs an informational message using the event metadata.
    /// </summary>
    /// <param name="message">Template or message text.</param>
    /// <param name="args">Optional structured logging arguments.</param>
    void LogInformation(string message, params object?[] args);
    /// <summary>
    /// Logs an informational message that includes the supplied exception.
    /// </summary>
    /// <param name="exception">Exception to attach.</param>
    /// <param name="message">Template or message text.</param>
    /// <param name="args">Optional structured logging arguments.</param>
    void LogInformation(Exception exception, string message, params object?[] args);

    /// <summary>
    /// Logs a warning message using the event metadata.
    /// </summary>
    /// <param name="message">Template or message text.</param>
    /// <param name="args">Optional structured logging arguments.</param>
    void LogWarning(string message, params object?[] args);
    /// <summary>
    /// Logs a warning message that includes the supplied exception.
    /// </summary>
    /// <param name="exception">Exception to attach.</param>
    /// <param name="message">Template or message text.</param>
    /// <param name="args">Optional structured logging arguments.</param>
    void LogWarning(Exception exception, string message, params object?[] args);

    /// <summary>
    /// Logs an error message using the event metadata.
    /// </summary>
    /// <param name="message">Template or message text.</param>
    /// <param name="args">Optional structured logging arguments.</param>
    void LogError(string message, params object?[] args);
    /// <summary>
    /// Logs an error message that includes the supplied exception.
    /// </summary>
    /// <param name="exception">Exception to attach.</param>
    /// <param name="message">Template or message text.</param>
    /// <param name="args">Optional structured logging arguments.</param>
    void LogError(Exception exception, string message, params object?[] args);

    /// <summary>
    /// Logs a critical message using the event metadata.
    /// </summary>
    /// <param name="message">Template or message text.</param>
    /// <param name="args">Optional structured logging arguments.</param>
    void LogCritical(string message, params object?[] args);
    /// <summary>
    /// Logs a critical message that includes the supplied exception.
    /// </summary>
    /// <param name="exception">Exception to attach.</param>
    /// <param name="message">Template or message text.</param>
    /// <param name="args">Optional structured logging arguments.</param>
    void LogCritical(Exception exception, string message, params object?[] args);
}
