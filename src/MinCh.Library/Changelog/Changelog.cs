using Vogen;

namespace MinCh.Library.Changelog;

public record ReleaseRecord(string Version, ReleaseDate Date, IReadOnlyList<ChangeGroup> Groups);

[ValueObject<DateOnly>]
public readonly partial struct ReleaseDate
{
    public static ReleaseDate On(int year, int month, int day)
    {
        return From(new DateOnly(year, month, day));
    }
}
