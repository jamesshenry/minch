using System.Text.RegularExpressions;
using MinCh.Library.Changelog;

namespace MinCh.Library.Git;

public class ParsedCommit
{
    public required string Description { get; init; }
    public string? Scope { get; init; }
    public required ConventionalCommitType Type { get; init; }
    public required bool IsBreaking { get; init; }
}

public static class ConventionalCommitParser
{
    public static IReadOnlyList<ParsedCommit> Parse(ChangeSet changeSet)
    {
        var parsedCommits = new List<ParsedCommit>();

        foreach (var commit in changeSet.Commits)
        {
            var match = Regex.Match(
                commit.Subject,
                @"^(?<type>\w+)(\((?<scope>.+)\))?:\s*(?<description>.+)$"
            );

            if (!match.Success)
                continue;

            if (
                !Enum.TryParse<ConventionalCommitType>(
                    match.Groups["type"].Value,
                    true,
                    out var type
                )
            )
                continue;

            var scope = match.Groups["scope"].Value;
            var description = match.Groups["description"].Value;

            parsedCommits.Add(
                new ParsedCommit
                {
                    Description = description,
                    Scope = string.IsNullOrEmpty(scope) ? null : scope,
                    Type = type,
                    IsBreaking = commit.IsBreaking,
                }
            );
        }

        return parsedCommits;
    }
}

public enum ConventionalCommitType
{
    Fix,
    Feat,
    Perf,
    Chore,
    Refactor,
}
