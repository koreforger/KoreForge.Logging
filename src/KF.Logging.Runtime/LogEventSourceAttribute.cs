using System;

namespace KF.Logging;

/// <summary>
/// Marks an enum as a log event source, allowing the generator to build strongly typed loggers.
/// </summary>
[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class LogEventSourceAttribute : Attribute
{
    /// <summary>
    /// Root logger type name to generate. Falls back to &lt;EnumName&gt;Logger&lt;T&gt; when null.
    /// </summary>
    public string? LoggerRootTypeName { get; set; }

    /// <summary>
    /// Namespace for generated loggers. Defaults to the enum namespace.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Optional prefix that is prepended to generated event paths.
    /// </summary>
    public string? BasePath { get; set; }

    /// <summary>
    /// Log levels to emit methods for. Default generator behavior may emit all levels.
    /// </summary>
    public LogLevels Levels { get; set; } = LogLevels.Default;
}
