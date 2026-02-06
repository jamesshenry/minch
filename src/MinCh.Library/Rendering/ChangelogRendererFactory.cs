namespace MinCh.Library.Rendering;

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

public interface IChangelogRenderer
{
    string Render(Changelog changelog);
}

internal class CommonChangelogRenderer : IChangelogRenderer
{
    public string Render(Changelog changelog)
    {
        throw new NotImplementedException();
    }
}
