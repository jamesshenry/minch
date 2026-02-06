using MinCh.Library.Git;

namespace MinCh.Library.Rendering;

public interface IChangeSetRenderer
{
    string Render(ChangeSet changeSet);
}
