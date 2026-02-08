using Vogen;

namespace MinCh.Library.Changelog;

public record ChangelogRecord(
    string Version,
    ChangelogDate Date,
    IReadOnlyList<ChangeGroup> Groups
);

[ValueObject<DateOnly>]
public readonly partial struct ChangelogDate
{
    public static ChangelogDate On(int year, int month, int day)
    {
        return From(new DateOnly(year, month, day));
    }
}
