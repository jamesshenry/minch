namespace MinCh.Library.Git;

public record ChangeSet
{
    public required Ref From { get; init; }
    public required Ref To { get; init; }

    public required bool IsDirty { get; init; }
    public required int CommitCount { get; init; }

    public IReadOnlyList<Commit> Commits { get; init; } = [];
    public IReadOnlyList<string> Files { get; init; } = [];
}
