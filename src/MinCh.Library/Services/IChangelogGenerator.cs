using MinCh.Library.Git;

namespace MinCh.Library.Services;

public interface IChangelogGenerator
{
    Task<Changelog> GenerateAsync(ChangeSet changeSet, string version);
}

public class CommonChangelogGenerator(TimeProvider time) : IChangelogGenerator
{
    private readonly TimeProvider _time = time;

    public async Task<Changelog> GenerateAsync(ChangeSet changeSet, string version)
    {
        var sections = ChangeSetParser.Parse(changeSet);
        return new Changelog(version, DateOnly.FromDateTime(_time.GetUtcNow().DateTime), sections);
    }
}

public static class ChangeSetParser
{
    public static IReadOnlyList<ChangeGroup> Parse(ChangeSet changeSet)
    {
        throw new NotImplementedException();
    }
}
