using MinCh.Library.Git;
using Vogen;

namespace MinCh.Library.Changelog;

public record Release(string Version, ReleaseDate Date, IReadOnlyList<ParsedCommit> Commits);

[ValueObject<DateOnly>]
public readonly partial struct ReleaseDate
{
    public static ReleaseDate On(int year, int month, int day)
    {
        return From(new DateOnly(year, month, day));
    }
}
