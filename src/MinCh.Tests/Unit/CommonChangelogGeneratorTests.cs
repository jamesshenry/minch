// using MinCh.Library.Changelog;
// using MinCh.Library.Git;
// using NSubstitute;

// namespace MinCh.Library.Tests.Services;

// public class CommonChangelogGeneratorTests
// {
//     private readonly TimeProvider _timeProvider;
//     private readonly CommonChangelogGenerator _generator;

//     public CommonChangelogGeneratorTests()
//     {
//         _timeProvider = Substitute.For<TimeProvider>();
//         _generator = new CommonChangelogGenerator(_timeProvider);
//     }

//     [Test]
//     public async Task GenerateAsync_ShouldReturnChangelogWithCorrectVersionAndDate()
//     {
//         // Arrange
//         var version = "1.0.0";
//         var utcNow = new DateTimeOffset(2026, 2, 6, 10, 0, 0, TimeSpan.Zero);
//         _timeProvider.GetUtcNow().Returns(utcNow);

//         var fromRef = new Ref
//         {
//             Name = "main",
//             Kind = GitRefKind.Branch,
//             CommitSha = "sha1",
//         };
//         var toRef = new Ref
//         {
//             Name = "head",
//             Kind = GitRefKind.Branch,
//             CommitSha = "sha2",
//         };
//         var changeSet = new ChangeSet
//         {
//             From = fromRef,
//             To = toRef,
//             IsDirty = false,
//             CommitCount = 0,
//             Commits = [],
//             Files = [],
//         };

//         // Act
//         var changelog = await _generator.Generate(changeSet, version);

//         // Assert
//         await Assert.That(changelog.Version).IsEqualTo(version);
//         await Assert.That(changelog.Date.Value).IsEqualTo(DateOnly.FromDateTime(utcNow.DateTime));
//     }

//     [Test]
//     public async Task GenerateAsync_ShouldReturnChangelogWithNoGroupsForEmptyChangeSet()
//     {
//         // Arrange
//         var version = "1.0.0";
//         var utcNow = new DateTimeOffset(2026, 2, 6, 10, 0, 0, TimeSpan.Zero);
//         _timeProvider.GetUtcNow().Returns(utcNow);

//         var fromRef = new Ref
//         {
//             Name = "main",
//             Kind = GitRefKind.Branch,
//             CommitSha = "sha1",
//         };
//         var toRef = new Ref
//         {
//             Name = "head",
//             Kind = GitRefKind.Branch,
//             CommitSha = "sha2",
//         };
//         var changeSet = new ChangeSet
//         {
//             From = fromRef,
//             To = toRef,
//             IsDirty = false,
//             CommitCount = 0,
//             Commits = [],
//             Files = [],
//         };

//         // Act
//         var changelog = await _generator.Generate(changeSet, version);

//         // Assert
//         await Assert.That(changelog.Commits).IsNotNull();
//         await Assert.That(changelog.Commits).IsEmpty();
//     }

//     [Test]
//     public async Task GenerateAsync_ShouldReturnChangelogWithGroupsForNonEmptyChangeSet()
//     {
//         // Arrange
//         var version = "1.0.0";
//         var utcNow = new DateTimeOffset(2026, 2, 6, 10, 0, 0, TimeSpan.Zero);
//         _timeProvider.GetUtcNow().Returns(utcNow);

//         var commit = new Commit
//         {
//             Sha = "abc",
//             Author = "Test Author",
//             Date = DateTime.Now,
//             Subject = "feat: subject",
//             IsMerge = false,
//             IsBreaking = false,
//         };
//         var fromRef = new Ref
//         {
//             Name = "main",
//             Kind = GitRefKind.Branch,
//             CommitSha = "sha1",
//         };
//         var toRef = new Ref
//         {
//             Name = "head",
//             Kind = GitRefKind.Branch,
//             CommitSha = "sha2",
//         };
//         var changeSet = new ChangeSet
//         {
//             From = fromRef,
//             To = toRef,
//             IsDirty = false,
//             CommitCount = 1,
//             Commits = [commit],
//             Files = [],
//         };

//         // Act
//         var release = await _generator.Generate(changeSet, version);

//         // Assert
//         await Assert.That(release.Commits).IsNotNull();
//         await Assert.That(release.Commits).IsNotEmpty();
//         await Assert.That(release.Commits.Count()).IsEqualTo(1); // Assuming one group for a single "feat" commit
//         await Assert.That(release.Commits[0].Title).IsEqualTo("Features");
//         await Assert.That(release.Commits[0].Items.Count()).IsEqualTo(1);
//         await Assert.That(release.Commits[0].Items[0].Description).IsEqualTo("subject");
//     }
// }
