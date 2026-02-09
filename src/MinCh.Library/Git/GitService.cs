using System.Diagnostics;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Logging;
using MinCh.Library.Git;

namespace MinCh.Library.Git;

public partial class GitService(ILogger<GitService> logger, string gitExecutablePath = "git")
    : IGitService
{
    private string _workingDirectory = Directory.GetCurrentDirectory();
    private readonly ILogger<GitService> _logger = logger;
    private readonly string _gitExecutablePath = gitExecutablePath;

    public void SetWorkingDirectory(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        _workingDirectory = path;
    }

    public async Task<Ref> ResolveRefAsync(string refName)
    {
        // Check for special references first (HEAD, etc.) to avoid matching pattern refs like refs/remotes/origin/HEAD
        if (refName.Any(c => c is '~' or '@' or '^' or ':') || refName == "HEAD")
        {
            var rawSha =
                await GetRefShaAsync($"{refName}^{{commit}}") ?? await GetRefShaAsync(refName);
            if (rawSha != null)
            {
                return ResolveRawRef(refName, rawSha);
            }

            throw new InvalidOperationException(
                $"'{refName}' is not a valid git reference. It may reference a commit that doesn't exist in your history."
            );
        }

        var searchPatterns = new (string Path, GitRefKind Kind)[]
        {
            ($"refs/tags/{refName}", GitRefKind.Tag),
            ($"refs/heads/{refName}", GitRefKind.Branch),
        };

        foreach (var pattern in searchPatterns)
        {
            var sha = await GetRefShaAsync($"{pattern.Path}^{{commit}}");

            if (sha != null)
            {
                return new Ref
                {
                    Name = refName,
                    Kind = pattern.Kind,
                    CommitSha = sha,
                };
            }
        }

        throw new InvalidOperationException(
            $"'{refName}' is not a valid git reference. Try running 'git fetch --all'."
        );
    }

    private static Ref ResolveRawRef(string name, string sha) =>
        name.ToUpperInvariant() switch
        {
            "HEAD" => new Ref
            {
                Name = "HEAD",
                Kind = GitRefKind.Special,
                CommitSha = sha,
            },
            _ => new Ref
            {
                Name = name,
                Kind = GitRefKind.Commit,
                CommitSha = sha,
            },
        };

    public async Task<bool> IsDirtyAsync()
    {
        var output = await RunGitCliWrapAsync("status --porcelain");
        return !string.IsNullOrWhiteSpace(output);
    }

    public async Task<string?> GetLastTagAsync()
    {
        try
        {
            var output = await RunGitCliWrapAsync("describe --tags --abbrev=0");
            return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<Commit>> GetCommitsAsync(Ref fromRef, Ref toRef)
    {
        var range = $"{fromRef.CommitSha}..{toRef.CommitSha}";
        var output = await RunGitCliWrapAsync(
            $"log --format=%H%x1f%P%x1f%an%x1f%ai%x1f%B%x1e {range}"
        );

        if (string.IsNullOrWhiteSpace(output))
            return [];

        var commits = new List<Commit>();
        var entries = output.Split(
            ['\u001e'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        foreach (var commitEntry in entries)
        {
            var commitLines = commitEntry.Split(['\u001f']);

            if (DateTime.TryParse(commitLines[3].Trim(), out var date))
            {
                commits.Add(
                    new Commit
                    {
                        Sha = commitLines[0],
                        IsMerge = commitLines[1].Contains(' '),
                        IsBreaking =
                            commitLines[4].Contains("BREAKING") || commitLines[4].Contains("!:"),
                        Author = commitLines[2],
                        Date = date,
                        Subject = commitLines[4],
                    }
                );
            }
        }

        return commits;
    }

    public async Task<IReadOnlyList<string>> GetFilesAsync(Ref fromRef, Ref toRef)
    {
        var range = $"{fromRef.CommitSha}..{toRef.CommitSha}";
        var output = await RunGitCliWrapAsync($"diff --name-only {range}");

        if (string.IsNullOrWhiteSpace(output))
            return [];

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();
    }

    private async Task<string?> GetRefShaAsync(string refPath)
    {
        try
        {
            var output = await RunGitCliWrapAsync($"rev-parse {refPath}");
            return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
        }
        catch
        {
            return null;
        }
    }

    private bool IsValidCommitSha(string sha)
    {
        return sha.Length >= 7 && sha.All(c => char.IsAsciiHexDigit(c));
    }

    private async Task<string> RunGitCliWrapAsync(string arguments)
    {
        var cmd = Cli.Wrap(_gitExecutablePath)
            .WithArguments(arguments)
            .WithWorkingDirectory(_workingDirectory);

        var result = await cmd.ExecuteBufferedAsync();

        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Git command failed: {result.StandardError}");

        return result.StandardOutput.Trim();
    }

    public async Task<string> GetRepoRootAsync()
    {
        var output = await RunGitCliWrapAsync($"rev-parse --show-toplevel");

        return output;
    }
}
