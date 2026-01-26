using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Spectre.Console;

namespace MinCh.Filters;

internal sealed class ExceptionFilter(ConsoleAppFilter next, ILoggerFactory factory)
    : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(
        ConsoleAppContext context,
        CancellationToken cancellationToken
    )
    {
        var logger = factory.CreateLogger("Program");

        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Program stopped");
        }
    }
}

internal class ServiceProviderScopeFilter(IServiceProvider serviceProvider, ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(
        ConsoleAppContext context,
        CancellationToken cancellationToken
    )
    {
        // create Microsoft.Extensions.DependencyInjection scope
        await using var scope = serviceProvider.CreateAsyncScope();
        var levelSwitch = scope.ServiceProvider.GetRequiredService<LoggingLevelSwitch>();
        GlobalOptions globalOptions = (context.GlobalOptions as GlobalOptions)!;
        if (globalOptions.Verbose)
        {
            levelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Debug;
        }
        // replace static ServiceProvider
        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        catch { }
    }
}
