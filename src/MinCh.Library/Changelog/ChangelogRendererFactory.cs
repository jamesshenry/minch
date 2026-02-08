namespace MinCh.Library.Changelog;

public class ChangelogRendererFactory
{
    public IChangelogRenderer Get(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "commonchangelog" => new CommonChangelogRenderer(),
            _ => throw new ArgumentException($"Unknown output format: {format}"),
        };
    }
}
