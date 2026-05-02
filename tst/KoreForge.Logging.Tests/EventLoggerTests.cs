using System.Collections.Generic;
using System.Linq;
using KoreForge.Logging.Internal;
using Microsoft.Extensions.Logging;
using Xunit;

namespace KoreForge.Logging.Tests;

/// <summary>
/// Verifies behavior of the runtime <see cref="KoreForge.Logging.Internal.EventLogger"/>.
/// </summary>
public sealed class EventLoggerTests
{
    /// <summary>
    /// Ensures informational logs capture the event scope and event id metadata.
    /// </summary>
    [Fact]
    public void LogInformation_EmitsScopeAndEvent()
    {
        var loggerDouble = new TestLogger();
        var logger = new EventLogger(loggerDouble, 42, "MyApp.App.Start");

        logger.LogInformation("Starting");

        Assert.Equal(LogLevel.Information, loggerDouble.LastLevel);
        Assert.Equal(42, loggerDouble.LastEventId?.Id);
        Assert.Equal("MyApp.App.Start", loggerDouble.LastEventId?.Name);
        Assert.Equal("Starting", loggerDouble.LastFormattedMessage);
        Assert.Equal("MyApp.App.Start", loggerDouble.LastScopeValues?["EventPath"]);
    }

    private sealed class TestLogger : ILogger
    {
        public LogLevel? LastLevel { get; private set; }
        public EventId? LastEventId { get; private set; }
        public string? LastFormattedMessage { get; private set; }
        public IReadOnlyDictionary<string, object?>? LastScopeValues { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                LastScopeValues = pairs.ToDictionary(p => p.Key, p => p.Value);
            }

            return new Scope();
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LastLevel = logLevel;
            LastEventId = eventId;
            LastFormattedMessage = formatter(state, exception);
        }

        private sealed class Scope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
