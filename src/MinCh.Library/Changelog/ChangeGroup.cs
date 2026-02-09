using MinCh.Library.Git;

namespace MinCh.Library.Changelog;

public record ChangeGroup(string Title, IReadOnlyList<ParsedCommit> Items);
