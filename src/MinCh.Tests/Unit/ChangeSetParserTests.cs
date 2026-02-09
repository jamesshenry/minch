using MinCh.Library.Changelog;
using MinCh.Library.Git;
using MinCh.Library.Git;
using TUnit.Assertions.Extensions;

namespace MinCh.Library.Tests.Services;

public class ChangeSetParserTests
{
    [Test]
    public async Task Parse_ShouldReturnEmptyListForEmptyChangeSet()
    {
        // Arrange
        var fromRef = new Ref
        {
            Name = "main",
            Kind = GitRefKind.Branch,
            CommitSha = "sha1",
        };
        var toRef = new Ref
        {
            Name = "head",
            Kind = GitRefKind.Branch,
            CommitSha = "sha2",
        };
        var changeSet = new ChangeSet
        {
            From = fromRef,
            To = toRef,
            IsDirty = false,
            CommitCount = 0,
            Commits = new List<Commit>(),
            Files = new List<string>(),
        };

        // Act
        var result = ConventionalCommitParser.Parse(changeSet);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task Parse_ShouldGroupCommitsByType()
    {
        // Arrange
        var commit1 = new Commit
        {
            Sha = "abc1",
            Author = "A",
            Date = DateTime.Now,
            Subject = "feat: new feature",
            IsMerge = false,
            IsBreaking = false,
        };
        var commit2 = new Commit
        {
            Sha = "abc2",
            Author = "B",
            Date = DateTime.Now,
            Subject = "fix: bug fix",
            IsMerge = false,
            IsBreaking = false,
        };
        var commit3 = new Commit
        {
            Sha = "abc3",
            Author = "C",
            Date = DateTime.Now,
            Subject = "chore: clean up",
            IsMerge = false,
            IsBreaking = false,
        };

        var fromRef = new Ref
        {
            Name = "main",
            Kind = GitRefKind.Branch,
            CommitSha = "sha1",
        };
        var toRef = new Ref
        {
            Name = "head",
            Kind = GitRefKind.Branch,
            CommitSha = "sha2",
        };
        var changeSet = new ChangeSet
        {
            From = fromRef,
            To = toRef,
            IsDirty = false,
            CommitCount = 3,
            Commits = new List<Commit> { commit1, commit2, commit3 },
            Files = new List<string>(),
        };

        // Act
        var result = ConventionalCommitParser.Parse(changeSet);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count()).IsEqualTo(3);

        await Assert.That(result.Any(g => g.Title == "Features")).IsTrue();
        await Assert.That(result.Any(g => g.Title == "Bug Fixes")).IsTrue();
        await Assert.That(result.Any(g => g.Title == "Chores")).IsTrue();

        await Assert.That(result.First(g => g.Title == "Features").Items.Count()).IsEqualTo(1);
        await Assert.That(result.First(g => g.Title == "Bug Fixes").Items.Count()).IsEqualTo(1);
        await Assert.That(result.First(g => g.Title == "Chores").Items.Count()).IsEqualTo(1);
    }

    [Test]
    public async Task Parse_ShouldHandleBreakingChanges()
    {
        // Arrange
        var commit1 = new Commit
        {
            Sha = "abc1",
            Author = "A",
            Date = DateTime.Now,
            Subject = "feat: new feature",
            IsMerge = false,
            IsBreaking = true,
        };
        var commit2 = new Commit
        {
            Sha = "abc2",
            Author = "B",
            Date = DateTime.Now,
            Subject = "fix: bug fix",
            IsMerge = false,
            IsBreaking = false,
        };

        var fromRef = new Ref
        {
            Name = "main",
            Kind = GitRefKind.Branch,
            CommitSha = "sha1",
        };
        var toRef = new Ref
        {
            Name = "head",
            Kind = GitRefKind.Branch,
            CommitSha = "sha2",
        };
        var changeSet = new ChangeSet
        {
            From = fromRef,
            To = toRef,
            IsDirty = false,
            CommitCount = 2,
            Commits = new List<Commit> { commit1, commit2 },
            Files = new List<string>(),
        };

        // Act
        var result = ConventionalCommitParser.Parse(changeSet);

        // Assert
        await Assert.That(result.Count()).IsEqualTo(3); // Breaking Changes, Features, Bug Fixes

        await Assert.That(result.Any(g => g.Title == "Breaking Changes")).IsTrue();
        await Assert
            .That(result.First(g => g.Title == "Breaking Changes").Items.Count())
            .IsEqualTo(1);
        await Assert
            .That(result.First(g => g.Title == "Breaking Changes").Items[0].Description)
            .IsEqualTo("feat: new feature");
    }
}
