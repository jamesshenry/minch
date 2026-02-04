using Microsoft.Extensions.Logging.Abstractions;
using MinCh.Library.Git;
using MinCh.Library.Services;

namespace MinCh.Tests.Integration;

[ClassDataSource<SMBStableFixture>] // PerTest ensures a fresh clone every time
public class Scenario1IntegrationTests(SMBStableFixture fixture)
{
    [Test]
    public async Task S1_ResolveRef_WhenUsingLastTag_ShouldReturnV110()
    {
        // Arrange
        var service = new GitService(NullLogger<GitService>.Instance);
        service.SetWorkingDirectory(fixture.RepoPath);
        var builder = new ChangeSetBuilder(service, NullLogger<ChangeSetBuilder>.Instance);

        // Act
        var changeSet = await builder.BuildAsync(from: "last-tag", to: "HEAD");

        // Assert
        // In Scenario 1, the last tag created is v1.1.0
        await Assert.That(changeSet.From.Name).IsEqualTo("v1.1.0");
        await Assert.That(changeSet.From.Kind).IsEqualTo(GitRefKind.Tag);
    }

    [Test]
    public async Task S1_GetCommits_FromV100_ToV101_ShouldOnlyIncludeHotfix()
    {
        // Arrange
        var service = new GitService(NullLogger<GitService>.Instance);
        service.SetWorkingDirectory(fixture.RepoPath);
        var builder = new ChangeSetBuilder(service, NullLogger<ChangeSetBuilder>.Instance);

        // Act
        var changeSet = await builder.BuildAsync(from: "v1.0.0", to: "v1.0.1");

        // Assert
        // Between v1.0.0 and v1.0.1, only the hotfix commit should appear
        await Assert
            .That(changeSet.Commits)
            .Any(c => c.Subject.Contains("fix: client socket timeout crash"));
        // Should NOT include feature commits (those are in develop)
        await Assert.That(changeSet.Commits).All(c => !c.Subject.Contains("feat:"));
    }

    [Test]
    public async Task S1_GetCommits_FromV101_ToV110_ShouldIncludeFeatureAndSyncCommit()
    {
        // Arrange
        var service = new GitService(NullLogger<GitService>.Instance);
        service.SetWorkingDirectory(fixture.RepoPath);
        var builder = new ChangeSetBuilder(service, NullLogger<ChangeSetBuilder>.Instance);

        // Act
        var changeSet = await builder.BuildAsync(from: "v1.0.1", to: "v1.1.0");

        // Assert
        // Between v1.0.1 and v1.1.0, should include feature commits and sync commit
        await Assert.That(changeSet.Commits).Any(c => c.Subject.Contains("feat:"));
        await Assert.That(changeSet.Commits).Any(c => c.Subject.Contains("chore: sync hotfix"));
        // Should include the release merge commit
        await Assert.That(changeSet.Commits).Any(c => c.Subject.Contains("chore: release v1.1.0"));
    }

    [Test]
    public async Task S1_BuildChangeSet_ShouldDetectMergeCommits_WhenNoFFUsed()
    {
        // Arrange
        var service = new GitService(NullLogger<GitService>.Instance);
        service.SetWorkingDirectory(fixture.RepoPath);
        var builder = new ChangeSetBuilder(service, NullLogger<ChangeSetBuilder>.Instance);

        // Act
        var changeSet = await builder.BuildAsync(from: "v1.0.0", to: "v1.1.0");

        // Assert
        // Scenario 1 uses --no-ff for all merges, creating merge commits
        // Should detect multiple merge commits in the history
        var mergeCommits = changeSet.Commits.Where(c => c.IsMerge).ToList();
        await Assert.That(mergeCommits.Count).IsGreaterThanOrEqualTo(3); // Feature merge, hotfix merge, release merge
    }
}
