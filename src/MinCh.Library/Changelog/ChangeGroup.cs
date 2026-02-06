namespace MinCh.Library;

public record ChangeGroup(string Title, IReadOnlyList<ChangeItem> Items);
