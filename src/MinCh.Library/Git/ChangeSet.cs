namespace MinCh.Library.Git;

public record ChangeSet
{
    public Ref From { get; init; }
    public Ref To { get; init; }

    public bool IsDirty { get; init; }
    public int CommitCount { get; init; }

    public IReadOnlyList<Commit> Commits { get; init; }
    public IReadOnlyList<string> Files { get; init; }
}
