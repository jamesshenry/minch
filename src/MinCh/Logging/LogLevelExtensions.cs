using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace MinCh.Logging;

public static class LogLevelExtensions
{
    public static LogEventLevel ToSerilogLevel(this VerbosityLevel verbosity) =>
        verbosity switch
        {
            VerbosityLevel.Quiet => (LogEventLevel)6,
            VerbosityLevel.Minimal => LogEventLevel.Error,
            VerbosityLevel.Normal => LogEventLevel.Warning,
            VerbosityLevel.Detailed => LogEventLevel.Information,
            VerbosityLevel.Verbose => LogEventLevel.Debug,
            _ => LogEventLevel.Warning,
        };

    public static LogLevel ToMicrosoftLevel(this VerbosityLevel verbosity) =>
        verbosity switch
        {
            VerbosityLevel.Quiet => LogLevel.None,
            VerbosityLevel.Minimal => LogLevel.Error,
            VerbosityLevel.Normal => LogLevel.Warning,
            VerbosityLevel.Detailed => LogLevel.Information,
            VerbosityLevel.Verbose => LogLevel.Debug,
            _ => LogLevel.Warning,
        };
}
