using ConsoleAppFramework;
using MinCh.Filters;
using MinCh.Infrastructure;
using MinCh.Logging;
using MinCh.Services;
using Serilog;
using Velopack;

Log.Logger = ServiceExtensions.CreateAppLogger();

try
{
#if !PUBLISH_AS_TOOL
    if (OperatingSystem.IsWindows())
    {
        VelopackApp
            .Build()
            .OnAfterInstallFastCallback(v => StartupTasks.Install(v))
            .OnBeforeUninstallFastCallback(v => StartupTasks.Uninstall(v))
            .Run();
    }
#endif

    var app = ConsoleApp
        .Create()
        .ConfigureGlobalOptions(
            (ref builder) =>
            {
                var verbosity = builder.AddGlobalOption(
                    "-v|--verbosity",
                    "",
                    VerbosityLevel.Minimal
                );
                return new GlobalOptions(verbosity);
            }
        );
    ;
    app.ConfigureEmptyConfiguration(configure => configure.CreateConfiguration())
        .ConfigureServices(
            (context, configuration, services) =>
            {
                services.RegisterAppServices(configuration, Log.Logger);
            }
        );

    app.UseFilter<LoggingLevelFilter>();
    await app.RunAsync(args);
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
