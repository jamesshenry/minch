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

    public static IConfigurationBuilder CreateConfiguration(
        this IConfigurationBuilder configuration
    )
    {
        return configuration.AddJsonFile("config.json", optional: false, reloadOnChange: true);
    }

    public static Logger CreateAppLogger() =>
        new LoggerConfiguration()
            .MinimumLevel.Information()
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
        services.AddSerilog(logger: appLogger, dispose: appLogger is null);
        services.AddSingleton(configuration);
        services.AddSingleton<IGitService, GitService>();
        services.AddSingleton<RendererFactory>();
        services.AddTransient<IChangeSetBuilder, ChangeSetBuilder>();
        services.AddSingleton<MinchCommands>();

        return services;
    }
}
