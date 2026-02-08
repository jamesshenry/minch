namespace MinCh.Library.Changelog;

public record ChangeGroup(string Title, IReadOnlyList<ChangeItem> Items);
