using Microsoft.Extensions.Logging;

namespace MinCh.Logging;

/// <summary>
/// Source-generated logging methods for MinCh application.
/// </summary>
public static partial class AppLogger
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Creating default config: {Path}"
    )]
    public static partial void CreatingDefaultConfig(this ILogger logger, string path);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Verbosity set to {Level}")]
    public static partial void VerbositySet(this ILogger logger, string level);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Performing installation tasks..."
    )]
    public static partial void PerformingInstallationTasks(this ILogger logger);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Adding path to $env.PATH: {Directory}"
    )]
    public static partial void AddingPathToEnvironment(this ILogger logger, string directory);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Add path result: {Status}"
    )]
    public static partial void AddPathResult(this ILogger logger, string status);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Cleaning up path...")]
    public static partial void CleaningUpPath(this ILogger logger);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "Remove from path result: {Status}"
    )]
    public static partial void RemovePathResult(this ILogger logger, string status);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Debug,
        Message = "Loading configuration from {Path}"
    )]
    public static partial void LoadingConfiguration(this ILogger logger, string path);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Debug,
        Message = "Application terminating unexpectedly"
    )]
    public static partial void ApplicationTerminated(this ILogger logger, Exception? ex = null);
}
