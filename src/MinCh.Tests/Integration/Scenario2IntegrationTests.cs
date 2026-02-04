using Microsoft.Extensions.Logging.Abstractions;
using MinCh.Library.Git;
using MinCh.Library.Services;

namespace MinCh.Tests.Integration;

[ClassDataSource<LTSSupportFixture>] // PerTest ensures a fresh clone every time
public class Scenario2IntegrationTests(LTSSupportFixture fixture)
{
    [Test]
    public async Task S2_ChangeSetBuilder_ShouldMarkAsBreaking_WhenFeatExclamationFound()
    {
        // Arrange
        var service = new GitService(NullLogger<GitService>.Instance);
        service.SetWorkingDirectory(fixture.RepoPath);
        var builder = new ChangeSetBuilder(service, NullLogger<ChangeSetBuilder>.Instance);

        // Act
        var changeSet = await builder.BuildAsync(from: "v1.0.1", to: "v2.0.0");

        // Assert
        // Scenario 2: feat!: new db schema should be detected as a breaking change
        await Assert
            .That(changeSet.Commits)
            .Any(c => c.Subject.Contains("feat!:") && c.IsBreaking == true);
    }

    [Test]
    public async Task S2_GetCommits_BetweenV101_AndV111_ShouldOnlyShowBackportedFix()
    {
        // Arrange
        var service = new GitService(NullLogger<GitService>.Instance);
        service.SetWorkingDirectory(fixture.RepoPath);
        var builder = new ChangeSetBuilder(service, NullLogger<ChangeSetBuilder>.Instance);

        // Act
        var changeSet = await builder.BuildAsync(from: "v1.0.1", to: "v1.1.1");

        // Assert
        // Between v1.0.1 and v1.1.1 on support/v1.x, should only include:
        // - support policy docs commit
        // - security patch (backported from main)
        // Should NOT include v2 features (new db schema, cloud sync, etc.)
        await Assert
            .That(changeSet.Commits)
            .Any(c => c.Subject.Contains("fix: encrypt local logs"));
        await Assert
            .That(changeSet.Commits)
            .Any(c => c.Subject.Contains("docs: update v1 support policy"));
        await Assert
            .That(changeSet.Commits)
            .All(c => !c.Subject.Contains("feat!:") && !c.Subject.Contains("Cloud Logic"));
    }

    [Test]
    public async Task S2_ResolveRef_ShouldHandleSupportBranchNames()
    {
        // Arrange
        var service = new GitService(NullLogger<GitService>.Instance);
        service.SetWorkingDirectory(fixture.RepoPath);

        // Act
        var supportRef = await service.ResolveRefAsync("support/v1.x");

        // Assert
        // support/v1.x branch should be resolvable
        await Assert.That(supportRef.Kind).IsEqualTo(GitRefKind.Branch);
        await Assert.That(supportRef.Name).IsEqualTo("support/v1.x");
        await Assert.That(supportRef.CommitSha.Length).IsEqualTo(40);
    }

    [Test]
    public async Task S2_Compare_V200_To_V201_ShouldShowSecurityPatchOnly()
    {
        // Arrange
        var service = new GitService(NullLogger<GitService>.Instance);
        service.SetWorkingDirectory(fixture.RepoPath);
        var builder = new ChangeSetBuilder(service, NullLogger<ChangeSetBuilder>.Instance);

        // Act
        var changeSet = await builder.BuildAsync(from: "v2.0.0", to: "v2.0.1");

        // Assert
        // Between v2.0.0 and v2.0.1, should only include the security patch
        // Should NOT include feature commits (dark mode, etc.)
        await Assert
            .That(changeSet.Commits)
            .Any(c => c.Subject.Contains("fix: encrypt local logs"));
        await Assert
            .That(changeSet.Commits)
            .All(c => !c.Subject.Contains("feat:") && !c.Subject.Contains("Dark Mode"));
    }
}
