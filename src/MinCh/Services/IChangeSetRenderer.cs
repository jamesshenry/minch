using MinCh.Library;
using MinCh.Library.Git;

namespace MinCh.Services;

public interface IChangeSetRenderer
{
    void Render(ChangeSet changeSet);
}
