namespace MinCh.Library.Changelog;

public interface IChangelogRenderer
{
    string Render(ChangelogRecord changelog);
}

public class CommonChangelogRenderer : IChangelogRenderer
{
    public string Render(ChangelogRecord changelog)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# Changelog {changelog.Version} ({changelog.Date.Value:yyyy-MM-dd})");
        sb.AppendLine();

        for (int i = 0; i < changelog.Groups.Count; i++)
        {
            var group = changelog.Groups[i];
            sb.AppendLine($"## {group.Title}");
            sb.AppendLine();
            foreach (var item in group.Items)
            {
                sb.AppendLine($"* {item.Description}");
            }

            if (i < changelog.Groups.Count - 1)
            {
                sb.AppendLine();
            }
        }

        var result = sb.ToString();
        return changelog.Groups.Count > 0 ? result.TrimEnd('\r', '\n') : result;
    }
}
