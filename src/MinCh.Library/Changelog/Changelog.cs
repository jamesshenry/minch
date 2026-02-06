namespace MinCh.Library;

public record Changelog(string Version, DateOnly Date, IReadOnlyList<ChangeGroup> Groups);
