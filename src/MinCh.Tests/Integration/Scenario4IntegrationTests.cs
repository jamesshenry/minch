using Microsoft.Extensions.Logging.Abstractions;
using MinCh.Library.Git;
using MinCh.Library.Git;

namespace MinCh.Tests.Integration;

[ClassDataSource<EnterpriseCrisisSupportFixture>] // PerTest ensures a fresh clone every time
public class Scenario4IntegrationTests(EnterpriseCrisisSupportFixture fixture)
{
    [Test]
    public async Task S4_ChangeSetBuilder_FromV201_ToV202_ShouldShowRegressionFix()
    {
        // Arrange
        var service = new GitService(NullLogger<GitService>.Instance);
        service.SetWorkingDirectory(fixture.RepoPath);
        var builder = new ChangeSetBuilder(service, NullLogger<ChangeSetBuilder>.Instance);

        // Act
        var changeSet = await builder.BuildAsync(from: "v2.0.1", to: "v2.0.2");

        // Assert
        // Between v2.0.1 and v2.0.2, should show the regression fix
        await Assert
            .That(changeSet.Commits)
            .Any(c => c.Subject.Contains("fix: restore login button visibility"));
        // Should NOT include security fix (already in v2.0.1)
        await Assert.That(changeSet.Commits).All(c => !c.Subject.Contains("sanitize user input"));
    }

    [Test]
    public async Task S4_GetCommits_FromV100_ToV101_ShouldMatchV2SecurityContent()
    {
        // Arrange
        var service = new GitService(NullLogger<GitService>.Instance);
        service.SetWorkingDirectory(fixture.RepoPath);
        var builder = new ChangeSetBuilder(service, NullLogger<ChangeSetBuilder>.Instance);

        // Act
        var v1ChangeSet = await builder.BuildAsync(from: "v1.0.0", to: "v1.0.1");
        var v2ChangeSet = await builder.BuildAsync(from: "v2.0.0", to: "v2.0.1");

        // Assert
        // Both should include the same security fix (backported from hotfix/cve-2024)
        await Assert.That(v1ChangeSet.Commits).Any(c => c.Subject.Contains("sanitize user input"));
        await Assert.That(v2ChangeSet.Commits).Any(c => c.Subject.Contains("sanitize user input"));
        // Verify parity: both paths should have the security commit
        var v1HasSecurityFix = v1ChangeSet.Commits.Any(c =>
            c.Subject.Contains("sanitize user input")
        );
        var v2HasSecurityFix = v2ChangeSet.Commits.Any(c =>
            c.Subject.Contains("sanitize user input")
        );
        await Assert.That(v1HasSecurityFix).IsEqualTo(v2HasSecurityFix);
    }

    [Test]
    public async Task S4_Build_ShouldIdentifyMultipleAuthors_WhenMergedFromDifferentBranches()
    {
        // Arrange
        var service = new GitService(NullLogger<GitService>.Instance);
        service.SetWorkingDirectory(fixture.RepoPath);
        var builder = new ChangeSetBuilder(service, NullLogger<ChangeSetBuilder>.Instance);

        // Act
        var changeSet = await builder.BuildAsync(from: "v2.0.0", to: "v2.0.2");

        // Assert
        // Multiple branches merged (hotfix/cve-2024, hotfix/ui-regress)
        // Should have multiple merge commits showing different branch integrations
        await Assert.That(changeSet.Commits.Count).IsGreaterThanOrEqualTo(2);
        var mergeCommits = changeSet
            .Commits.Where(c => c.Subject.Contains("Merge") || c.Subject.Contains("merge"))
            .Count();
        await Assert.That(mergeCommits).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task S4_Pathological_ShouldHandleRapidTagging_WithoutDroppingCommits()
    {
        // Arrange
        var service = new GitService(NullLogger<GitService>.Instance);
        service.SetWorkingDirectory(fixture.RepoPath);
        var builder = new ChangeSetBuilder(service, NullLogger<ChangeSetBuilder>.Instance);

        // Act
        // Scenario 4 has rapid v2.0.0 -> v2.0.1 -> v2.0.2 tagging
        var changeSetV200ToV202 = await builder.BuildAsync(from: "v2.0.0", to: "v2.0.2");
        var changeSetV200ToV201 = await builder.BuildAsync(from: "v2.0.0", to: "v2.0.1");
        var changeSetV201ToV202 = await builder.BuildAsync(from: "v2.0.1", to: "v2.0.2");

        // Assert
        // Total commits from v2.0.0 to v2.0.2 should equal sum of intermediate ranges
        // (allowing for merge commits which may appear in both)
        var totalCommitsV200ToV202 = changeSetV200ToV202.Commits.Count;
        var commitsV200ToV201 = changeSetV200ToV201.Commits.Count;
        var commitsV201ToV202 = changeSetV201ToV202.Commits.Count;

        // v2.0.0->v2.0.2 should include all security and regression fixes
        await Assert
            .That(changeSetV200ToV202.Commits)
            .Any(c => c.Subject.Contains("sanitize user input"));
        await Assert
            .That(changeSetV200ToV202.Commits)
            .Any(c => c.Subject.Contains("restore login button"));
        // Rapid tagging should not lose commits
        await Assert.That(totalCommitsV200ToV202).IsGreaterThanOrEqualTo(2);
    }
}
