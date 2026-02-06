namespace MinCh.Library.Rendering;

public class ChangeSetRendererFactory
{
    public IChangeSetRenderer Get(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "json" => new JsonChangeSetRenderer(),
            "text" => new TextChangeSetRenderer(),
            _ => throw new ArgumentException($"Unknown output format: {format}"),
        };
    }
}
