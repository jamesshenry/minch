using MinCh.Library.Git;

namespace MinCh.Library.Changelog;

public interface IChangelogRenderer
{
    string Render(Release changelog);
}

public class CommonChangelogRenderer : IChangelogRenderer
{
    public string Render(Release release)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"## {release.Version} - {release.Date.Value:yyyy-MM-dd}");
        sb.AppendLine();

        // Group commits by breaking, then by type
        var breakingCommits = release.Commits.Where(c => c.IsBreaking).ToList();
        var featureCommits = release
            .Commits.Where(c => !c.IsBreaking && c.Type == ConventionalCommitType.Feat)
            .ToList();
        var fixCommits = release
            .Commits.Where(c => !c.IsBreaking && c.Type == ConventionalCommitType.Fix)
            .ToList();
        var choreCommits = release
            .Commits.Where(c => !c.IsBreaking && c.Type == ConventionalCommitType.Chore)
            .ToList();
        var perfCommits = release
            .Commits.Where(c => !c.IsBreaking && c.Type == ConventionalCommitType.Perf)
            .ToList();
        var refactorCommits = release
            .Commits.Where(c => !c.IsBreaking && c.Type == ConventionalCommitType.Refactor)
            .ToList();

        var groups = new (string Category, List<ParsedCommit> Commits)[]
        {
            ("Added", featureCommits),
            ("Fixed", fixCommits),
            ("Changed", [.. perfCommits, .. refactorCommits]),
        };

        var hasAnyCommits = false;
        foreach (var (category, commits) in groups)
        {
            if (commits.Count == 0)
                continue;

            hasAnyCommits = true;
            sb.AppendLine($"### {category}");
            sb.AppendLine();
            foreach (var commit in commits)
            {
                sb.AppendLine($"- {commit.Type.ToString().ToLower()}: {commit.Description}");
            }
            sb.AppendLine();
        }

        var result = sb.ToString();
        return hasAnyCommits ? result.TrimEnd('\r', '\n') : result;
    }
}
