using MinCh.Library.Git;

namespace MinCh.Library.Services;

public interface IGitService
{
    void DoSomething();
    ChangeSet GetChangeSet(Ref fromRef, Ref toRef);
    IReadOnlyList<Commit> GetCommits(Ref fromRef, Ref toRef);
    IReadOnlyList<string> GetFiles(Ref fromRef, Ref toRef);
    bool IsDirty();
    Ref ResolveRef(string from);

    /// <summary>
    /// Gets the most recent tag reachable from HEAD using git describe.
    /// </summary>
    /// <returns>The tag name, or null if no tags exist.</returns>
    string? GetLastTag();
}
