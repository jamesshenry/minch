using CliWrap;
using TUnit.Core.Interfaces;

namespace MinCh.Tests;

public abstract class GitBundleFixture(string bundleName) : IAsyncInitializer, IAsyncDisposable
{
    public string RepoPath { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        RepoPath = Path.Combine(Path.GetTempPath(), "minch-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(RepoPath);
        var bundlePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", bundleName);

        await Cli.Wrap("git")
            .WithArguments(["clone", bundlePath, "."])
            .WithWorkingDirectory(RepoPath)
            .ExecuteAsync();

        await Cli.Wrap("git")
            .WithArguments(["config", "user.name", "Test"])
            .WithWorkingDirectory(RepoPath)
            .ExecuteAsync();
        await Cli.Wrap("git")
            .WithArguments(["config", "user.email", "test@test.com"])
            .WithWorkingDirectory(RepoPath)
            .ExecuteAsync();

        foreach (var file in Directory.EnumerateFiles(RepoPath, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (string.IsNullOrEmpty(RepoPath) || !Directory.Exists(RepoPath))
            return;

        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Directory.Delete(RepoPath, recursive: true);
        }
        catch { }
    }
}

public class SMBStableFixture() : GitBundleFixture("scenario-1.bundle");

public class LTSSupportFixture() : GitBundleFixture("scenario-2.bundle");

public class SelectiveReleaseSupportFixture() : GitBundleFixture("scenario-3.bundle");

public class EnterpriseCrisisSupportFixture() : GitBundleFixture("scenario-4.bundle");
