using System.Text.Json;
using DotNetPathUtils;
using MinCh.Configuration;
using NuGet.Versioning;
using Serilog;
using Velopack.Locators;
using Velopack.Logging;

namespace MinCh.Infrastructure;

public static class StartupTasks
{
    private static JsonSerializerOptions JsonOptions { get; } =
        new JsonSerializerOptions { WriteIndented = true };

    internal static void Install(SemanticVersion? v = null)
    {
        var logger = VelopackLocator.CreateDefaultForPlatform().Log;

        logger.Info("Performing installation tasks...");
        logger.Info($"Adding path to $env.PATH: {AppContext.BaseDirectory} ");

        var appDir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
        var result = new PathEnvironmentHelper().EnsureDirectoryIsInPath(appDir);

        logger.Info($"Add path result: {result.Status}");
    }

    internal static void Uninstall(SemanticVersion? v = null)
    {
        var logger = VelopackLocator.CreateDefaultForPlatform().Log;
        logger.Info("Performing installation tasks...");
        logger.Info("Cleaning up path...");

        var appDir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
        var result = new PathEnvironmentHelper().RemoveDirectoryFromPath(appDir);

        logger.Info($"Remove from path result: {result.Status}");
    }

    public static async Task InitializeAsync(Serilog.ILogger? logger = null)
    {
        Directory.CreateDirectory(AppPaths.ConfigHome);
        Directory.CreateDirectory(AppPaths.StateHome);
        Directory.CreateDirectory(AppPaths.DataHome);

        var configPath = Path.Combine(AppPaths.ConfigHome, "config.json");
        if (!File.Exists(configPath))
        {
            logger?.Information("Creating default config: {Path}", configPath);
            await File.WriteAllTextAsync(
                configPath,
                JsonSerializer.Serialize(AppConfigContext.Default.AppConfig)
            );
        }

        await RefreshStateAsync(logger);
    }

    private static async Task RefreshStateAsync(ILogger? logger)
    {
        var statePath = Path.Combine(AppPaths.StateHome, "state.json");
        var currentVersion =
            VelopackLocator.CreateDefaultForPlatform().CurrentlyInstalledVersion?.ToString()
            ?? "0.0.0";

        AppState state = new();

        if (File.Exists(statePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(statePath);
                state = JsonSerializer.Deserialize<AppState>(json) ?? state;
            }
            catch { }
        }
        else
        {
            logger?.Information("First run detected. Initializing state.");
        }

        if (state.LastRunVersion != currentVersion)
        {
            state.LastRunVersion = currentVersion;
            await File.WriteAllTextAsync(statePath, JsonSerializer.Serialize(state, JsonOptions));
        }

        if (state.LastRunVersion != currentVersion)
        {
            state.LastRunVersion = currentVersion;
            await File.WriteAllTextAsync(statePath, JsonSerializer.Serialize(state, JsonOptions));
        }
    }
}
