namespace MinCh.Library.Changelog;

public interface IChangelogRenderer
{
    string Render(ReleaseRecord changelog);
}

public class CommonChangelogRenderer : IChangelogRenderer
{
    public string Render(ReleaseRecord release)
    {
        var sb = new System.Text.StringBuilder();
        // sb.AppendLine("# Changelog");
        sb.AppendLine($"## [{release.Version}] - {release.Date.Value:yyyy-MM-dd}");
        sb.AppendLine();

        for (int i = 0; i < release.Groups.Count; i++)
        {
            var group = release.Groups[i];
            sb.AppendLine($"## {group.Title}");
            sb.AppendLine();
            foreach (var item in group.Items)
            {
                sb.AppendLine($"- {item.Description}");
            }

            if (i < release.Groups.Count - 1)
            {
                sb.AppendLine();
            }
        }

        var result = sb.ToString();
        return release.Groups.Count > 0 ? result.TrimEnd('\r', '\n') : result;
    }
}
