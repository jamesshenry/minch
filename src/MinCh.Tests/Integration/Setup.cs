using System.Diagnostics;

namespace MinCh.Tests.Integration;

public class GlobalHooks
{
    private static string? _root;

    [Before(Class)]
    public static async Task SetUp()
    {
        // TODO: Spin up git repos to test against. Use kdl to store git commands to be run sequentially
        _root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var generator = new GitRepoGenerator(
            templatePath: Path.Combine(_root, "templateRepo"),
            baseDir: Path.Combine(_root, "testRepos")
        );
        await generator.GenerateReposAsync(count: 10, extraCommitsPerRepo: 5);
    }

    [After(TestSession)]
    public static void CleanUp()
    {
        if (_root != null)
        {
            Directory.Delete(_root);
        }
    }
}

class GitRepoGenerator
{
    private readonly string _templatePath;
    private readonly string _baseDir;
    private readonly int _maxConcurrency;

    public GitRepoGenerator(string templatePath, string baseDir, int? maxConcurrency = null)
    {
        _templatePath = templatePath;
        _baseDir = baseDir;
        _maxConcurrency = maxConcurrency ?? Environment.ProcessorCount;
    }

    public async Task GenerateReposAsync(int count, int extraCommitsPerRepo = 3)
    {
        Directory.CreateDirectory(_baseDir);

        using var semaphore = new SemaphoreSlim(_maxConcurrency);
        var tasks = Enumerable
            .Range(1, count)
            .Select(async i =>
            {
                await semaphore.WaitAsync();
                try
                {
                    string repoPath = Path.Combine(_baseDir, $"repo{i}");
                    await CloneTemplateAsync(repoPath);
                    await AddRandomCommitsAsync(repoPath, extraCommitsPerRepo);
                }
                finally
                {
                    semaphore.Release();
                }
            });

        await Task.WhenAll(tasks);
    }

    private Task CloneTemplateAsync(string targetPath)
    {
        Directory.CreateDirectory(targetPath);
        return RunGitAsync($"clone \"{_templatePath}\" .", targetPath);
    }

    private async Task AddRandomCommitsAsync(string repoPath, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            string fileName = $"file{i}.txt";
            string content = $"Random content {Guid.NewGuid()}";
            await File.WriteAllTextAsync(Path.Combine(repoPath, fileName), content);
            await RunGitAsync($"add .", repoPath);
            await RunGitAsync($"commit -m \"Random commit {i}\"", repoPath);
        }
    }

    private Task RunGitAsync(string args, string workingDir)
    {
        var tcs = new TaskCompletionSource<object>();

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = workingDir,
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
                tcs.SetException(new Exception($"Git failed: {error}"));
            }
            else
            {
                tcs.SetResult(null);
            }
            process.Dispose();
        };

        process.Start();
        return tcs.Task;
    }
}
