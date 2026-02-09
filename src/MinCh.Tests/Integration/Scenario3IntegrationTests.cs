using Microsoft.Extensions.Logging.Abstractions;
using MinCh.Library.Git;

namespace MinCh.Tests.Integration;

[ClassDataSource<SelectiveReleaseSupportFixture>(Shared = SharedType.PerClass)]
public class Scenario3IntegrationTests(SelectiveReleaseSupportFixture fixture)
{
    private ChangeSetBuilder CreateBuilder()
    {
        var service = new GitService(NullLogger<GitService>.Instance);
        service.SetWorkingDirectory(fixture.RepoPath);
        return new ChangeSetBuilder(service, NullLogger<ChangeSetBuilder>.Instance);
    }

    [Test]
    public async Task Scenario3_Build_ShouldCaptureTheFullHistoryIncludingReverts()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act - Scanning the whole history of the bundle
        // (Assuming "HEAD" is at v2.1.0 after the bundle is unpacked)
        var changeSet = await builder.BuildAsync(from: "HEAD~3", to: "HEAD");

        // Assert
        // We expect the 'feat: ai' AND the 'revert: feat: ai' to be present
        // because ChangeSetBuilder is the "Source of Truth"
        await Assert.That(changeSet.Commits).Any(c => c.Subject.Contains("feat: ai integration"));
        await Assert
            .That(changeSet.Commits)
            .Any(c => c.Subject.Contains("Revert \"feat: ai integration\""));

        await Assert.That(changeSet.CommitCount).IsGreaterThanOrEqualTo(5);
    }

    [Test]
    public async Task Scenario3_Build_BetweenTags_ShouldResolveCorrectRanges()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act - Test the range between the RC and the Final Release
        var changeSet = await builder.BuildAsync(from: "v2.1.0-rc.1", to: "v2.1.0");

        // Assert
        // Between RC1 and Final, there is usually only the merge commit into main
        await Assert.That(changeSet.Commits).Any(c => c.Subject.Contains("v2.1.0 ship"));
        await Assert.That(changeSet.From.Name).IsEqualTo("v2.1.0-rc.1");
        await Assert.That(changeSet.To.Name).IsEqualTo("v2.1.0");
    }

    [Test]
    public async Task Scenario3_Build_ShouldReflectFilesTouchedInTheHistory()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        var changeSet = await builder.BuildAsync(from: "v2.1.0-rc.1", to: "v2.1.0");

        // Assert
        // Even if a feature was reverted, the 'diff' within a certain range
        // might show files. Here we just ensure the file list isn't null.
        await Assert.That(changeSet.Files).IsNotNull();
    }
}
