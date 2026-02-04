namespace MinCh.Library.Git;

public record Commit
{
    public required string Sha { get; init; }
    public required string Author { get; init; }
    public DateTime Date { get; init; }
    public bool IsMerge { get; init; }
    public required string Subject { get; init; }
    public bool IsBreaking { get; init; }
}
