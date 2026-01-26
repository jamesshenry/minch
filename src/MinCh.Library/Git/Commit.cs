namespace MinCh.Library.Git;

public record Commit
{
    public string Sha { get; init; }
    public string Author { get; init; }
    public DateTime Date { get; init; }
    public string Subject { get; init; }
}
