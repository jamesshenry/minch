using System.Text.RegularExpressions;
using MinCh.Library.Changelog;
using MinCh.Library.Git;

namespace MinCh.Library.Services;

public static class ChangeSetParser
{
    public static IReadOnlyList<ChangeGroup> Parse(ChangeSet changeSet)
    {
        List<ChangeItem> breakingChanges = [];
        List<ChangeItem> featureItems = [];
        List<ChangeItem> fixItems = [];
        List<ChangeItem> choreItems = [];

        foreach (var commit in changeSet.Commits)
        {
            if (commit.IsBreaking)
            {
                breakingChanges.Add(new ChangeItem(commit.Subject, null));
            }

            var match = Regex.Match(
                commit.Subject,
                @"^(?<type>\w+)(\((?<scope>.+)\))?:\s*(?<description>.+)$"
            );
            if (match.Success)
            {
                var type = match.Groups["type"].Value.ToLowerInvariant();
                var scope = match.Groups["scope"].Value;
                var description = match.Groups["description"].Value;

                var item = new ChangeItem(description, string.IsNullOrEmpty(scope) ? null : scope);

                switch (type)
                {
                    case "feat":
                        featureItems.Add(item);
                        break;
                    case "fix":
                        fixItems.Add(item);
                        break;
                    case "chore":
                        choreItems.Add(item);
                        break;
                }
            }
        }

        var groups = new List<ChangeGroup>();

        if (breakingChanges.Count > 0)
        {
            groups.Add(new ChangeGroup("Breaking Changes", breakingChanges));
        }

        if (featureItems.Count > 0)
        {
            groups.Add(new ChangeGroup("Features", featureItems));
        }

        if (fixItems.Count > 0)
        {
            groups.Add(new ChangeGroup("Bug Fixes", fixItems));
        }

        if (choreItems.Count > 0)
        {
            groups.Add(new ChangeGroup("Chores", choreItems));
        }

        return groups;
    }
}
