using System.Diagnostics;

[assembly: System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]

namespace MinCh.Tests;

/// <summary>
/// Global test hooks and shared infrastructure for all tests.
/// Creates a template Git repository once per test session that can be cloned for isolated tests.
/// </summary>
public static class GlobalHooks
{
    private static string? _templatePath;

    /// <summary>
    /// Path to the shared template repository. Contains baseline state:
    /// - Initial commit with file1.txt, tagged v1.0.0
    /// - Second commit with file2.txt, tagged v2.0.0
    /// - feature/test branch with additional commit
    /// - main branch at v2.0.0
    /// </summary>
    public static string TemplatePath =>
        _templatePath
        ?? throw new InvalidOperationException(
            "Template repo not initialized. Ensure [Before(TestSession)] has run."
        );

    [Before(TestSession)]
    public static async Task SetUpTestSession()
    {
        _templatePath = Path.Combine(Path.GetTempPath(), $"minch_template_{Guid.NewGuid()}");
        Directory.CreateDirectory(_templatePath);

        // Initialize repo with baseline state (explicitly set branch name for CI/CD compatibility)
        await RunGitAsync("init --initial-branch=main", _templatePath);
        await RunGitAsync("config user.email \"test@example.com\"", _templatePath);
        await RunGitAsync("config user.name \"Test User\"", _templatePath);

        // First commit + tag
        await File.WriteAllTextAsync(Path.Combine(_templatePath, "file1.txt"), "Initial content");
        await RunGitAsync("add .", _templatePath);
        await RunGitAsync("commit -m \"Initial commit\"", _templatePath);
        await RunGitAsync("tag v1.0.0", _templatePath);

        // Second commit + tag
        await File.WriteAllTextAsync(
            Path.Combine(_templatePath, "file2.txt"),
            "Second commit content"
        );
        await RunGitAsync("add .", _templatePath);
        await RunGitAsync("commit -m \"Second commit\"", _templatePath);
        await RunGitAsync("tag v2.0.0", _templatePath);

        // Create feature branch
        await RunGitAsync("checkout -b feature/test", _templatePath);
        await File.WriteAllTextAsync(
            Path.Combine(_templatePath, "file3.txt"),
            "Feature commit content"
        );
        await RunGitAsync("add .", _templatePath);
        await RunGitAsync("commit -m \"Feature commit\"", _templatePath);

        // Switch back to main (use checkout, not checkout -b, since main already exists from git init)
        await RunGitAsync("checkout main", _templatePath);
    }

    [After(TestSession)]
    public static void CleanUpTestSession()
    {
        CleanupDirectory(_templatePath);
        _templatePath = null;
    }

    /// <summary>
    /// Clones the template repository to a new directory for tests that need isolation.
    /// Use this for tests that modify repo state (dirty tests, file operations, etc.)
    /// </summary>
    public static async Task<string> CloneTemplateAsync(string? suffix = null)
    {
        var destPath = Path.Combine(
            Path.GetTempPath(),
            $"minch_test_{suffix ?? Guid.NewGuid().ToString()}"
        );

        if (Directory.Exists(destPath))
        {
            CleanupDirectory(destPath);
        }

        // Use git clone --local for speed (uses hardlinks when possible)
        await RunGitAsync($"clone --local \"{TemplatePath}\" \"{destPath}\"", Path.GetTempPath());

        // Configure cloned repo
        await RunGitAsync("config user.email \"test@example.com\"", destPath);
        await RunGitAsync("config user.name \"Test User\"", destPath);

        return destPath;
    }

    /// <summary>
    /// Runs a git command asynchronously.
    /// </summary>
    public static async Task RunGitAsync(string arguments, string workingDirectory)
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
                tcs.SetException(new Exception($"Git command failed: git {arguments}\n{error}"));
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

    /// <summary>
    /// Safely cleans up a directory, handling locked files from git processes.
    /// </summary>
    public static void CleanupDirectory(string? path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return;

        try
        {
            // Force GC to release any file handles
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Remove read-only attributes (git creates read-only files)
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}
