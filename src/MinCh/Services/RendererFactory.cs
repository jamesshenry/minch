using System.Text.Json;
using MinCh.Library.Git;

namespace MinCh.Services;

public class RendererFactory
{
    public IChangeSetRenderer GetRenderer(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "json" => new JsonRenderer(),
            "text" => new TextRenderer(),
            // future formats: "markdown" => new MarkdownRenderer(),
            _ => throw new ArgumentException($"Unknown output format: {format}"),
        };
    }
}

internal class TextRenderer : IChangeSetRenderer
{
    public void Render(ChangeSet changeSet)
    {
        Console.WriteLine($"Changes from {changeSet.From.Name} to {changeSet.To.Name}");
        foreach (var commit in changeSet.Commits)
            Console.WriteLine($"- {commit.Subject}");
    }
}

internal class JsonRenderer : IChangeSetRenderer
{
    public void Render(ChangeSet changeSet)
    {
        var json = JsonSerializer.Serialize(
            changeSet,
            new JsonSerializerOptions { WriteIndented = true }
        );
        Console.WriteLine(json);
    }
}
