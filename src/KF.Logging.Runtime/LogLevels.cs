using System;

namespace KF.Logging;

/// <summary>
/// Flags enum describing which log level methods the generator should emit.
/// </summary>
[Flags]
public enum LogLevels
{
    /// <summary>No generated methods.</summary>
    None        = 0,
    /// <summary>Includes trace-level methods.</summary>
    Trace       = 1 << 0,
    /// <summary>Includes debug-level methods.</summary>
    Debug       = 1 << 1,
    /// <summary>Includes information-level methods.</summary>
    Information = 1 << 2,
    /// <summary>Includes warning-level methods.</summary>
    Warning     = 1 << 3,
    /// <summary>Includes error-level methods.</summary>
    Error       = 1 << 4,
    /// <summary>Includes critical-level methods.</summary>
    Critical    = 1 << 5,

    /// <summary>Default emission set (Debug, Information, Warning, Error).</summary>
    Default     = Debug | Information | Warning | Error
}
