using MinCh.Library.Git;
using MinCh.Library.Rendering;

namespace MinCh.Library.Rendering;

public class TextChangeSetRenderer : IChangeSetRenderer
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
