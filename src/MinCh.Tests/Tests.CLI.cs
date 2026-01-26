using System.Diagnostics;
using System.Text.Json;

namespace MinCh.Tests.CLI;

/// <summary>
/// CLI Integration Tests
/// Tests the MinCh command-line interface including:
/// - Argument parsing (--from, --to, --output, --allow-dirty)
/// - Output format selection (text, json)
/// - Exit codes (0 for changes, 1 for no changes, >1 for errors)
/// - Error handling for invalid refs
///
/// NOTE: These tests require the CLI implementation to be complete in Program.cs and MinchCommands.cs
/// Current state: CLI handlers are incomplete (missing renderer.Render() call)
/// </summary>
[NotInParallel("CLI")]
public class CLIIntegrationTests
{
    private string _testRepoPath = null!;
    private string _appPath = null!;

    [Before(Test)]
    public async Task SetUpCliTests()
    {
        // Create a test repository
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"cli_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRepoPath);

        // Initialize repo with commits and tags
        await RunGitAsync("init", _testRepoPath);
        await RunGitAsync("config user.email \"test@example.com\"", _testRepoPath);
        await RunGitAsync("config user.name \"Test User\"", _testRepoPath);

        // Create first commit
        await File.WriteAllTextAsync(Path.Combine(_testRepoPath, "file1.txt"), "Content 1");
        await RunGitAsync("add .", _testRepoPath);
        await RunGitAsync("commit -m \"First commit\"", _testRepoPath);
        await RunGitAsync("tag v1.0.0", _testRepoPath);

        // Create second commit
        await File.WriteAllTextAsync(Path.Combine(_testRepoPath, "file2.txt"), "Content 2");
        await RunGitAsync("add .", _testRepoPath);
        await RunGitAsync("commit -m \"Second commit\"", _testRepoPath);
        await RunGitAsync("tag v2.0.0", _testRepoPath);

        // Path to the built MinCh executable
        _appPath = GetMinChExecutablePath();
    }

    [After(Test)]
    public void CleanUpCliTests()
    {
        try
        {
            // Force close any git/minch processes
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            if (Directory.Exists(_testRepoPath))
            {
                Directory.Delete(_testRepoPath, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    [Test]
    [Skip("CLI implementation incomplete")]
    public async Task CLI_DefaultRun_UsesLastTagToHEAD()
    {
        var result = await RunMinChAsync(null);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("v1.0.0");
    }

    [Test]
    [Skip("CLI implementation incomplete")]
    public async Task CLI_ExplicitFrom_UsesProvidedRef()
    {
        var result = await RunMinChAsync(new[] { "--from", "v1.0.0", "--to", "v2.0.0" });

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("Second commit");
    }

    [Test]
    [Skip("CLI implementation incomplete")]
    public async Task CLI_TextOutput_FormatsAsText()
    {
        var result = await RunMinChAsync(new[] { "--output", "text" });

        await Assert.That(result.ExitCode).IsEqualTo(0);
        // Text output should contain "Changes from"
        await Assert.That(result.Output).Contains("Changes");
    }

    [Test]
    [Skip("CLI implementation incomplete")]
    public async Task CLI_JsonOutput_FormatsAsValidJson()
    {
        var result = await RunMinChAsync(new[] { "--output", "json" });

        await Assert.That(result.ExitCode).IsEqualTo(0);

        // Verify it's valid JSON
        try
        {
            var json = JsonDocument.Parse(result.Output);
            await Assert.That(true).IsTrue();
        }
        catch
        {
            await Assert.That(false).IsTrue(); // JSON parsing failed
        }
    }

    [Test]
    [Skip("CLI implementation incomplete")]
    public async Task CLI_InvalidFromRef_ExitsWithError()
    {
        var result = await RunMinChAsync(new[] { "--from", "non-existent-ref" });

        await Assert.That(result.ExitCode).IsGreaterThan(1);
        await Assert.That(result.Error).Contains("Unable to resolve");
    }

    [Test]
    [Skip("CLI implementation incomplete")]
    public async Task CLI_InvalidOutputFormat_ExitsWithError()
    {
        var result = await RunMinChAsync(new[] { "--output", "invalid" });

        await Assert.That(result.ExitCode).IsGreaterThan(1);
        await Assert.That(result.Error).Contains("Unknown output format");
    }

    [Test]
    [Skip("CLI implementation incomplete")]
    public async Task CLI_DirtyRepoWithoutAllowDirty_ExitsWithError()
    {
        // Make repo dirty
        await File.WriteAllTextAsync(Path.Combine(_testRepoPath, "dirty.txt"), "Dirty content");

        var result = await RunMinChAsync(null);

        await Assert.That(result.ExitCode).IsGreaterThan(1);
        await Assert.That(result.Error).Contains("dirty");
    }

    [Test]
    [Skip("CLI implementation incomplete")]
    public async Task CLI_DirtyRepoWithAllowDirty_Succeeds()
    {
        // Make repo dirty
        await File.WriteAllTextAsync(Path.Combine(_testRepoPath, "dirty.txt"), "Dirty content");

        var result = await RunMinChAsync(new[] { "--allow-dirty" });

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("dirty");
    }

    [Test]
    [Skip("CLI implementation incomplete")]
    public async Task CLI_NoChangesBetweenRefs_ExitsWithOne()
    {
        // Run with same ref for from and to
        var result = await RunMinChAsync(new[] { "--from", "v1.0.0", "--to", "v1.0.0" });

        await Assert.That(result.ExitCode).IsEqualTo(1);
    }

    [Test]
    [Skip("CLI implementation incomplete")]
    public async Task CLI_ChangesDetected_ExitsWithZero()
    {
        var result = await RunMinChAsync(new[] { "--from", "v1.0.0", "--to", "v2.0.0" });

        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    [Test]
    [Skip("CLI implementation incomplete")]
    public async Task CLI_HelpFlag_DisplaysUsage()
    {
        var result = await RunMinChAsync(new[] { "--help" });

        await Assert.That(result.Output).Contains("help");
    }

    // ============ Helper Methods ============

    private string GetMinChExecutablePath()
    {
        var projectRoot = Path.Combine(
            Path.GetDirectoryName(typeof(CLIIntegrationTests).Assembly.Location)!,
            ".."
        );
        var exeName = OperatingSystem.IsWindows() ? "MinCh.exe" : "MinCh";
        return Path.Combine(projectRoot, "bin", "Debug", "net10.0", exeName);
    }

    private async Task<(int ExitCode, string Output, string Error)> RunMinChAsync(string[]? args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _appPath,
            WorkingDirectory = _testRepoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (args != null)
        {
            foreach (var arg in args)
            {
                psi.ArgumentList.Add(arg);
            }
        }

        using (var process = Process.Start(psi))
        {
            if (process == null)
                throw new InvalidOperationException("Failed to start MinCh process");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            return (process.ExitCode, output, error);
        }
    }

    private static async Task RunGitAsync(string arguments, string workingDirectory)
    {
        var tcs = new TaskCompletionSource<object?>();
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.Exited += (s, e) =>
        {
            if (process.ExitCode != 0)
            {
                string error = process.StandardError.ReadToEnd();
                tcs.SetException(new Exception($"Git command failed: {error}"));
            }
            else
            {
                tcs.SetResult(null);
            }
            process.Dispose();
        };

        process.Start();
        await tcs.Task;
    }
}
