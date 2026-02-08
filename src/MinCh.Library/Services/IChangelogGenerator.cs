using MinCh.Library.Changelog;
using MinCh.Library.Git;

namespace MinCh.Library.Services;

public interface IChangelogGenerator
{
    Task<ChangelogRecord> Generate(ChangeSet changeSet, string version);
}

public class CommonChangelogGenerator(TimeProvider time) : IChangelogGenerator
{
    private readonly TimeProvider _time = time;

    public Task<ChangelogRecord> Generate(ChangeSet changeSet, string version)
    {
        var now = _time.GetUtcNow();
        var groups = ChangeSetParser.Parse(changeSet);

        return Task.FromResult(
            new ChangelogRecord(version, ChangelogDate.On(now.Year, now.Month, now.Day), groups)
        );
    }
}
