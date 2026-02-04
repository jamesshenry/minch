using System.Text.Json;
using MinCh.Commands;
using MinCh.Library.Git;
using MinCh.Library.Services;

namespace MinCh.Services;

public class RendererFactory
{
    public IChangeSetRenderer GetRenderer(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "json" => new JsonRenderer(),
            "text" => new TextRenderer(),
            "markdown" => new MarkdownRenderer(),
            _ => throw new ArgumentException($"Unknown output format: {format}"),
        };
    }
}

internal class TextRenderer : IChangeSetRenderer
{
    public string Render(ChangeSet changeSet)
    {
        var output = new System.Text.StringBuilder();
        output.AppendLine($"Changes from {changeSet.From.Name} to {changeSet.To.Name}");
        foreach (var commit in changeSet.Commits)
            output.AppendLine($"- {commit.Subject}");
        return output.ToString();
    }
}

internal class JsonRenderer : IChangeSetRenderer
{
    public string Render(ChangeSet changeSet)
    {
        return JsonSerializer.Serialize(changeSet, ChangeSetContext.Default.ChangeSet);
    }
}

internal class MarkdownRenderer : IChangeSetRenderer
{
    public string Style => "CommonChangelog";

    public string Render(ChangeSet changeSet)
    {
        return "";
    }
}
