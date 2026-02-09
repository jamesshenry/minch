using MinCh.Library.Git;

namespace MinCh.Library.Changelog;

public interface IChangelogGenerator
{
    Task<ReleaseRecord> Generate(ChangeSet changeSet, string version);
}

public class CommonChangelogGenerator(TimeProvider time) : IChangelogGenerator
{
    private readonly TimeProvider _time = time;

    public Task<ReleaseRecord> Generate(ChangeSet changeSet, string version)
    {
        var now = _time.GetUtcNow();
        var groups = ConventionalCommitParser.Parse(changeSet);

        return Task.FromResult(
            new ReleaseRecord(version, ReleaseDate.On(now.Year, now.Month, now.Day), groups)
        );
    }
}
