using MinCh.Library.Git;

namespace MinCh.Services;

public interface IGitService
{
    void DoSomething();
    ChangeSet GetChangeSet(Ref fromRef, Ref toRef);
    IReadOnlyList<Commit> GetCommits(Ref fromRef, Ref toRef);
    IReadOnlyList<string> GetFiles(Ref fromRef, Ref toRef);
    bool IsDirty();
    Ref ResolveRef(string from);
}
