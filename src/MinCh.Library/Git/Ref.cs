namespace MinCh.Library.Git;

public record Ref
{
    public required string Name { get; init; }
    public required GitRefKind Kind { get; init; }
    public required string CommitSha { get; init; }
}

public enum GitRefKind
{
    Tag,
    Branch,
    Commit,
    Special,
}
