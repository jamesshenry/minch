using System.Diagnostics;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Logging;
using MinCh.Library.Git;

namespace MinCh.Library.Services;

public partial class GitService(ILogger<GitService> logger, string gitExecutablePath = "git")
    : IGitService
{
    private string _workingDirectory = Directory.GetCurrentDirectory();
    private readonly ILogger<GitService> _logger = logger;
    private readonly string _gitExecutablePath = gitExecutablePath;

    /// <summary>
    /// Sets the working directory for git operations. Used primarily for testing.
    /// </summary>
    public void SetWorkingDirectory(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        _workingDirectory = path;
    }

    public async Task<Ref> ResolveRefAsync(string refName)
    {
        // Try to resolve as a tag first
        var tagSha = await GetRefShaAsync($"refs/tags/{refName}");
        if (tagSha != null)
            return new Ref
            {
                Name = refName,
                Kind = GitRefKind.Tag,
                CommitSha = tagSha,
            };

        // Try as a branch
        var branchSha = await GetRefShaAsync($"refs/heads/{refName}");
        if (branchSha != null)
            return new Ref
            {
                Name = refName,
                Kind = GitRefKind.Branch,
                CommitSha = branchSha,
            };

        // Try as special ref (HEAD)
        if (refName == "HEAD")
        {
            var headSha = await GetRefShaAsync("HEAD");
            if (headSha != null)
                return new Ref
                {
                    Name = "HEAD",
                    Kind = GitRefKind.Special,
                    CommitSha = headSha,
                };
        }

        // Try as commit SHA (abbreviated or full)
        if (IsValidCommitSha(refName))
        {
            var fullSha = await GetFullCommitShaAsync(refName);
            if (fullSha != null)
                return new Ref
                {
                    Name = refName,
                    Kind = GitRefKind.Commit,
                    CommitSha = fullSha,
                };
        }

        throw new InvalidOperationException($"Unable to resolve ref: {refName}");
    }

    public async Task<bool> IsDirtyAsync()
    {
        var output = await RunGitCliWrapAsync("status --porcelain");
        return !string.IsNullOrWhiteSpace(output);
    }

    public async Task<string?> GetLastTagAsync()
    {
        try
        {
            // --abbrev=0 returns just the tag name without commit distance
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
        var output = await RunGitCliWrapAsync($"log --format=%H%n%an%n%ai%n%s {range}");

        if (string.IsNullOrWhiteSpace(output))
            return [];

        var commits = new List<Commit>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length; i += 4)
        {
            if (i + 3 < lines.Length)
            {
                if (DateTime.TryParse(lines[i + 2].Trim(), out var date))
                {
                    commits.Add(
                        new Commit
                        {
                            Sha = lines[i].Trim(),
                            Author = lines[i + 1].Trim(),
                            Date = date,
                            Subject = lines[i + 3].Trim(),
                        }
                    );
                }
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

    private async Task<string?> GetFullCommitShaAsync(string sha)
    {
        try
        {
            var output = await RunGitCliWrapAsync($"rev-parse {sha}^{{commit}}");
            return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
        }
        catch
        {
            return null;
        }
    }

    private bool IsValidCommitSha(string sha)
    {
        // Check if it looks like a git SHA (hex string, at least 7 chars)
        return sha.Length >= 7 && sha.All(c => "0123456789abcdefABCDEF".Contains(c));
    }

    private async Task<string> RunGitCliWrapAsync(string arguments)
    {
        var cmd = Cli.Wrap(_gitExecutablePath)
            .WithArguments(arguments)
            .WithWorkingDirectory(_workingDirectory);

        var result = await cmd.ExecuteBufferedAsync();

        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Git command failed: {result.StandardError}");

        return result.StandardOutput;
    }

    public async Task<string> GetRepoRootAsync()
    {
        try
        {
            var output = await RunGitCliWrapAsync($"rev-parse --show-toplevel");
            if (!File.Exists(output))
            {
                throw new FileNotFoundException(output);
            }
            return output;
        }
        catch
        {
            throw;
        }
    }
}
