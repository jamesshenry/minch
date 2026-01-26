using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinCh.Commands;
using MinCh.Configuration;
using MinCh.Library.Services;
using MinCh.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace MinCh.Services;

public static class ServiceExtensions
{
    private const string OutputTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] ({SourceClass}) {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Static switch for controlling log level at runtime.
    /// </summary>
    private static readonly LoggingLevelSwitch LogLevelSwitch = new(LogEventLevel.Information);

    /// <summary>
    /// Gets the logging level switch to allow runtime adjustments.
    /// </summary>
    public static LoggingLevelSwitch GetLogLevelSwitch() => LogLevelSwitch;

    public static IConfigurationBuilder CreateConfiguration(
        this IConfigurationBuilder configuration
    )
    {
        return configuration.AddJsonFile("config.json", optional: true, reloadOnChange: true);
    }

    public static Logger CreateAppLogger() =>
        new LoggerConfiguration()
            .MinimumLevel.ControlledBy(LogLevelSwitch)
            .WriteTo.Console()
            .WriteTo.File(
                formatter: new MessageTemplateTextFormatter(OutputTemplate),
                Path.Combine(AppPaths.StateHome, "logs", "app-.log"),
                restrictedToMinimumLevel: LogEventLevel.Debug,
                shared: true,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31
            )
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", "<APP NAME>")
            .Enrich.With<SourceClassEnricher>()
            .CreateLogger();

    public static IServiceCollection RegisterAppServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Serilog.ILogger? appLogger = null
    )
    {
        services.AddLogging();
        services.AddSerilog(logger: appLogger, dispose: appLogger is null);
        services.AddSingleton(LogLevelSwitch);
        services.AddSingleton(configuration);
        services.AddSingleton<IGitService, GitService>();
        services.AddSingleton<RendererFactory>();
        services.AddTransient<IChangeSetBuilder, ChangeSetBuilder>();
        services.AddSingleton<MinchCommands>();

        return services;
    }
}
