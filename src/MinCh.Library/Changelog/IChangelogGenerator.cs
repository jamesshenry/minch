using MinCh.Library.Git;

namespace MinCh.Library.Changelog;

public interface IChangelogGenerator
{
    Task<Release> Generate(ChangeSet changeSet, string version);
}

public class CommonChangelogGenerator(TimeProvider time) : IChangelogGenerator
{
    private readonly TimeProvider _time = time;

    public Task<Release> Generate(ChangeSet changeSet, string version)
    {
        var now = _time.GetUtcNow();
        var commits = ConventionalCommitParser.Parse(changeSet);

        return Task.FromResult(
            new Release(version, ReleaseDate.On(now.Year, now.Month, now.Day), commits)
        );
    }
}
