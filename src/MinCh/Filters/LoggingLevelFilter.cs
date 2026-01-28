using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinCh.Logging;
using Serilog.Core;

namespace MinCh.Filters;

internal class LoggingLevelFilter(IServiceProvider serviceProvider, ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(
        ConsoleAppContext context,
        CancellationToken cancellationToken
    )
    {
        var factory = serviceProvider.GetService<ILoggerFactory>();
        var logger = factory?.CreateLogger("Program");
        var levelSwitch = serviceProvider.GetRequiredService<LoggingLevelSwitch>();
        GlobalOptions options = (context.GlobalOptions as GlobalOptions)!;
        levelSwitch.MinimumLevel = options.Verbosity.ToSerilogLevel();
        logger?.LogDebug("Verbosity set to {Level}", levelSwitch.MinimumLevel);

        await Next.InvokeAsync(context, cancellationToken);
    }
}
