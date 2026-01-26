using ConsoleAppFramework;
using MinCh.Filters;
using MinCh.Infrastructure;
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

    await StartupTasks.InitializeAsync(Log.Logger);
#endif
    var app = ConsoleApp
        .Create()
        .ConfigureGlobalOptions(
            (ref builder) =>
            {
                var verbose = builder.AddGlobalOption<bool>("-v|--verbose", "", false);
                return new GlobalOptions(verbose);
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

    app.UseFilter<ExceptionFilter>();
    app.UseFilter<ServiceProviderScopeFilter>();
    await app.RunAsync(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup");
}
finally
{
    await Log.CloseAndFlushAsync();
}

internal record GlobalOptions(bool Verbose);
