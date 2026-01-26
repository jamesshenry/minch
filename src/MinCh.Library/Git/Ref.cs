namespace MinCh.Library.Git;

public record Ref
{
    public string Name { get; init; }
    public GitRefKind Kind { get; init; }
    public string CommitSha { get; init; }
}

public enum GitRefKind
{
    Tag,
    Branch,
    Commit,
    Special,
}
