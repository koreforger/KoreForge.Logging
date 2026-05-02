using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace KoreForge.Logging.Internal;

/// <summary>
/// Default implementation used by generated loggers to emit structured events.
/// </summary>
public sealed class EventLogger : IEventLogger
{
    private static readonly object?[] NoArgs = Array.Empty<object?>();

    private readonly ILogger _logger;
    private readonly EventId _eventId;
    private readonly string _eventPath;

    /// <summary>
    /// Creates a new <see cref="EventLogger"/>.
    /// </summary>
    /// <param name="logger">Concrete logger instance.</param>
    /// <param name="eventId">Numeric event identifier.</param>
    /// <param name="eventPath">Hierarchical event path.</param>
    public EventLogger(ILogger logger, int eventId, string eventPath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventId = new EventId(eventId, eventPath);
        _eventPath = eventPath ?? throw new ArgumentNullException(nameof(eventPath));
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel level) => _logger.IsEnabled(level);

    private void LogInternal(LogLevel level, Exception? exception, string message, object?[]? args)
    {
        if (!_logger.IsEnabled(level))
        {
            return;
        }

        using (_logger.BeginScope(new Dictionary<string, object?>
               {
                   ["EventPath"] = _eventPath
               }))
        {
            _logger.Log(level,
                        _eventId,
                        exception,
                        message,
                        args?.Length > 0 ? args : NoArgs);
        }
    }

    /// <inheritdoc />
    public void LogTrace(string message, params object?[] args) => LogInternal(LogLevel.Trace, null, message, args);

    /// <inheritdoc />
    public void LogTrace(Exception exception, string message, params object?[] args) => LogInternal(LogLevel.Trace, exception, message, args);

    /// <inheritdoc />
    public void LogDebug(string message, params object?[] args) => LogInternal(LogLevel.Debug, null, message, args);

    /// <inheritdoc />
    public void LogDebug(Exception exception, string message, params object?[] args) => LogInternal(LogLevel.Debug, exception, message, args);

    /// <inheritdoc />
    public void LogInformation(string message, params object?[] args) => LogInternal(LogLevel.Information, null, message, args);

    /// <inheritdoc />
    public void LogInformation(Exception exception, string message, params object?[] args) => LogInternal(LogLevel.Information, exception, message, args);

    /// <inheritdoc />
    public void LogWarning(string message, params object?[] args) => LogInternal(LogLevel.Warning, null, message, args);

    /// <inheritdoc />
    public void LogWarning(Exception exception, string message, params object?[] args) => LogInternal(LogLevel.Warning, exception, message, args);

    /// <inheritdoc />
    public void LogError(string message, params object?[] args) => LogInternal(LogLevel.Error, null, message, args);

    /// <inheritdoc />
    public void LogError(Exception exception, string message, params object?[] args) => LogInternal(LogLevel.Error, exception, message, args);

    /// <inheritdoc />
    public void LogCritical(string message, params object?[] args) => LogInternal(LogLevel.Critical, null, message, args);

    /// <inheritdoc />
    public void LogCritical(Exception exception, string message, params object?[] args) => LogInternal(LogLevel.Critical, exception, message, args);
}
