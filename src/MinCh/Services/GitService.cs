using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MinCh.Library.Git;

namespace MinCh.Services;

public class GitService(ILogger<GitService> logger) : IGitService
{
    private string _workingDirectory = Directory.GetCurrentDirectory();

    /// <summary>
    /// Sets the working directory for git operations. Used primarily for testing.
    /// </summary>
    public void SetWorkingDirectory(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        _workingDirectory = path;
    }

    public void DoSomething()
    {
        logger.LogCritical("I am doing something");
    }

    public Ref ResolveRef(string refName)
    {
        // Try to resolve as a tag first
        var tagSha = GetRefSha($"refs/tags/{refName}");
        if (tagSha != null)
            return new Ref
            {
                Name = refName,
                Kind = GitRefKind.Tag,
                CommitSha = tagSha,
            };

        // Try as a branch
        var branchSha = GetRefSha($"refs/heads/{refName}");
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
            var headSha = GetRefSha("HEAD");
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
            var fullSha = GetFullCommitSha(refName);
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

    public bool IsDirty()
    {
        var output = RunGit("status --porcelain");
        return !string.IsNullOrWhiteSpace(output);
    }

    public IReadOnlyList<Commit> GetCommits(Ref fromRef, Ref toRef)
    {
        var range = $"{fromRef.CommitSha}..{toRef.CommitSha}";
        var output = RunGit($"log --format=%H%n%an%n%ai%n%s {range}");

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

    public IReadOnlyList<string> GetFiles(Ref fromRef, Ref toRef)
    {
        var range = $"{fromRef.CommitSha}..{toRef.CommitSha}";
        var output = RunGit($"diff --name-only {range}");

        if (string.IsNullOrWhiteSpace(output))
            return [];

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();
    }

    public ChangeSet GetChangeSet(Ref fromRef, Ref toRef)
    {
        throw new NotImplementedException();
    }

    private string? GetRefSha(string refPath)
    {
        try
        {
            var output = RunGit($"rev-parse {refPath}");
            return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
        }
        catch
        {
            return null;
        }
    }

    private string? GetFullCommitSha(string sha)
    {
        try
        {
            var output = RunGit($"rev-parse {sha}^{{commit}}");
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

    private string RunGit(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = _workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (var process = Process.Start(psi))
        {
            if (process == null)
                throw new InvalidOperationException("Failed to start git process");

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Git command failed: {error}");

            return output;
        }
    }
}
