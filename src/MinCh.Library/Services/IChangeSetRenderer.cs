using MinCh.Library;
using MinCh.Library.Git;

namespace MinCh.Library.Services;

public interface IChangeSetRenderer
{
    string Render(ChangeSet changeSet);
}
