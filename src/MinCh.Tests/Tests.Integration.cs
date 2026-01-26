using Microsoft.Extensions.Logging;
using MinCh.Library.Git;
using MinCh.Library.Services;

namespace MinCh.Tests.Integration;

/// <summary>
/// Integration tests using synthetic Git repositories.
/// Tests Ref Resolution and ChangeSetBuilder against real git repos with various scenarios:
/// - Minimal repo (1 commit, 1 file)
/// - Branch/tag repo (multiple commits, branches, tags)
/// - Merge repo (branch merges, multiple authors)
/// - Dirty repo (staged/unstaged changes)
/// - Rename/delete repo (file operations)
/// - Detached HEAD / special states
/// </summary>
[NotInParallel("Integration")]
[Property("TestType", "Integration")]
public class IntegrationSyntheticReposTests
{
    private GitService _gitService = null!;
    private ChangeSetBuilder _builder = null!;
    private string _testRepoPath = null!;

    [Before(Test)]
    public async Task SetUpTestRepo()
    {
        var logger = LoggerFactory
            .Create(builder => builder.AddConsole())
            .CreateLogger<GitService>();
        _gitService = new GitService(logger);

        var builderLogger = LoggerFactory
            .Create(builder => builder.AddConsole())
            .CreateLogger<ChangeSetBuilder>();
        _builder = new ChangeSetBuilder(_gitService, builderLogger);

        // Each test creates its own specialized repo
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"synth_repo_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRepoPath);
    }

    [After(Test)]
    public void CleanUpTestRepo()
    {
        GlobalHooks.CleanupDirectory(_testRepoPath);
    }

    /// <summary>
    /// Minimal repository: 1 commit, 1 file
    /// </summary>
    [Test]
    public async Task MinimalRepo_SingleCommit_RefResolutionWorks()
    {
        await CreateMinimalRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        var headRef = await _gitService.ResolveRefAsync("HEAD");

        await Assert.That(headRef.Kind).IsEqualTo(GitRefKind.Special);
        await Assert.That(headRef.CommitSha.Length).IsEqualTo(40);
    }

    /// <summary>
    /// Branch/tag repository with multiple refs
    /// </summary>
    [Test]
    public async Task BranchTagRepo_MultipleRefs_AllResolvable()
    {
        await CreateBranchTagRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        var v1Ref = await _gitService.ResolveRefAsync("v1.0.0");
        var v2Ref = await _gitService.ResolveRefAsync("v2.0.0");
        var mainRef = await _gitService.ResolveRefAsync("main");

        await Assert.That(v1Ref.Kind).IsEqualTo(GitRefKind.Tag);
        await Assert.That(v2Ref.Kind).IsEqualTo(GitRefKind.Tag);
        await Assert.That(mainRef.Kind).IsEqualTo(GitRefKind.Branch);
        await Assert.That(v1Ref.CommitSha).IsNotEqualTo(v2Ref.CommitSha);
    }

    /// <summary>
    /// Branch/tag repository with change tracking
    /// </summary>
    [Test]
    public async Task BranchTagRepo_ChangeSetBetweenTags_CorrectCommitAndFileCount()
    {
        await CreateBranchTagRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        var changeSet = await _builder.BuildAsync("v1.0.0", "v2.0.0");

        await Assert.That(changeSet.CommitCount).IsGreaterThan(0);
        await Assert.That(changeSet.Commits.Count).IsGreaterThan(0);
        await Assert.That(changeSet.Files.Count).IsGreaterThan(0);
    }

    /// <summary>
    /// Merge repository with multiple authors and merge commits
    /// </summary>
    [Test]
    public async Task MergeRepo_WithMergeCommits_AllCommitsIncluded()
    {
        await CreateMergeRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        var changeSet = await _builder.BuildAsync("v1.0.0", "HEAD");

        // Should include commits from both main and feature branch
        await Assert.That(changeSet.CommitCount).IsGreaterThanOrEqualTo(2);
    }

    /// <summary>
    /// Dirty repository with staged and unstaged changes
    /// </summary>
    [Test]
    public async Task DirtyRepo_WithStagedAndUnstagedChanges_IsDirtyFlagSet()
    {
        await CreateBranchTagRepoAsync();

        // Create untracked file
        await File.WriteAllTextAsync(
            Path.Combine(_testRepoPath, "untracked.txt"),
            "untracked content"
        );

        _gitService.SetWorkingDirectory(_testRepoPath);

        var isDirty = _gitService.IsDirtyAsync();

        await Assert.That(isDirty).IsTrue();
    }

    /// <summary>
    /// Detached HEAD state
    /// </summary>
    [Test]
    public async Task DetachedHeadRepo_CheckoutSpecificTag_HeadResolvesCorrectly()
    {
        await CreateBranchTagRepoAsync();

        // Checkout specific tag to detach HEAD
        await GlobalHooks.RunGitAsync("checkout v1.0.0", _testRepoPath);

        _gitService.SetWorkingDirectory(_testRepoPath);

        var headRef = await _gitService.ResolveRefAsync("HEAD");
        var tagRef = await _gitService.ResolveRefAsync("v1.0.0");

        await Assert.That(headRef.CommitSha).IsEqualTo(tagRef.CommitSha);
    }

    /// <summary>
    /// File deletion tracking
    /// </summary>
    [Test]
    public async Task FileDeleteRepo_DeletedFileTracked_FileAppearsInDiff()
    {
        await CreateBranchTagRepoAsync();
        await GlobalHooks.RunGitAsync("tag v2.1.0", _testRepoPath);

        // Delete a file
        File.Delete(Path.Combine(_testRepoPath, "file2.txt"));
        await GlobalHooks.RunGitAsync("add .", _testRepoPath);
        await GlobalHooks.RunGitAsync("commit -m \"Delete file2\"", _testRepoPath);
        await GlobalHooks.RunGitAsync("tag v2.2.0", _testRepoPath);

        _gitService.SetWorkingDirectory(_testRepoPath);

        var changeSet = await _builder.BuildAsync("v2.0.0", "v2.2.0");

        await Assert.That(changeSet.Files).Contains("file2.txt");
    }

    // ============ Last-Tag Resolution Tests ============

    /// <summary>
    /// last-tag keyword resolves to most recent tag
    /// </summary>
    [Test]
    public async Task LastTag_MultipleTagsExist_ResolvesToMostRecentTag()
    {
        await CreateBranchTagRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        // v2.0.0 is the most recent tag
        var changeSet = await _builder.BuildAsync("last-tag", "HEAD");

        await Assert.That(changeSet.From.Name).IsEqualTo("v2.0.0");
        await Assert.That(changeSet.From.Kind).IsEqualTo(GitRefKind.Tag);
    }

    /// <summary>
    /// last-tag in repo with no tags throws
    /// </summary>
    [Test]
    public async Task LastTag_NoTagsExist_ThrowsInvalidOperationException()
    {
        await CreateMinimalRepoAsync(); // Minimal repo has no tags
        _gitService.SetWorkingDirectory(_testRepoPath);

        await Assert
            .That(async () => await _builder.BuildAsync("last-tag", "HEAD"))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// GetLastTag returns the most recent tag name
    /// </summary>
    [Test]
    public async Task GetLastTag_MultipleTagsExist_ReturnsMostRecentTagName()
    {
        await CreateBranchTagRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        var lastTag = await _gitService.GetLastTagAsync();

        await Assert.That(lastTag).IsEqualTo("v2.0.0");
    }

    // ============ Unicode and Special Character Tests ============

    /// <summary>
    /// Commit messages with Unicode characters are handled correctly
    /// </summary>
    [Test]
    public async Task Unicode_CommitMessageWithEmoji_HandledCorrectly()
    {
        await CreateBranchTagRepoAsync();
        await GlobalHooks.RunGitAsync("tag v2.1.0", _testRepoPath);

        // Add commit with Unicode/emoji in message
        await File.WriteAllTextAsync(Path.Combine(_testRepoPath, "unicode.txt"), "🚀 content");
        await GlobalHooks.RunGitAsync("add .", _testRepoPath);
        await GlobalHooks.RunGitAsync("commit -m \"🚀 Add rocket feature\"", _testRepoPath);
        await GlobalHooks.RunGitAsync("tag v2.2.0", _testRepoPath);

        _gitService.SetWorkingDirectory(_testRepoPath);

        var changeSet = await _builder.BuildAsync("v2.1.0", "v2.2.0");

        await Assert.That(changeSet.CommitCount).IsEqualTo(1);
        await Assert.That(changeSet.Commits[0].Subject).StartsWith("🚀 Add rocket feature");
    }

    /// <summary>
    /// Tags with special characters in names
    /// </summary>
    [Test]
    public async Task SpecialChars_TagWithDots_ResolvesCorrectly()
    {
        await CreateMinimalRepoAsync();
        await GlobalHooks.RunGitAsync("tag release-1.0.0-beta.1", _testRepoPath);

        _gitService.SetWorkingDirectory(_testRepoPath);

        var ref_ = await _gitService.ResolveRefAsync("release-1.0.0-beta.1");

        await Assert.That(ref_.Kind).IsEqualTo(GitRefKind.Tag);
        await Assert.That(ref_.Name).IsEqualTo("release-1.0.0-beta.1");
    }

    // ============ Ambiguous Ref Tests ============

    /// <summary>
    /// When a tag and branch have the same name, tag takes precedence
    /// </summary>
    [Test]
    public async Task AmbiguousRef_TagAndBranchSameName_TagTakesPrecedence()
    {
        await CreateMinimalRepoAsync();

        // Create a branch named "release"
        await GlobalHooks.RunGitAsync("checkout -b release", _testRepoPath);
        await File.WriteAllTextAsync(Path.Combine(_testRepoPath, "branch.txt"), "branch content");
        await GlobalHooks.RunGitAsync("add .", _testRepoPath);
        await GlobalHooks.RunGitAsync("commit -m \"Branch commit\"", _testRepoPath);

        // Go back to main and create a tag with the same name "release"
        await GlobalHooks.RunGitAsync("checkout main", _testRepoPath);
        await GlobalHooks.RunGitAsync("tag release", _testRepoPath);

        _gitService.SetWorkingDirectory(_testRepoPath);

        // ResolveRef("release") should pick the tag (our implementation checks tags first)
        var ref_ = await _gitService.ResolveRefAsync("release");

        await Assert.That(ref_.Kind).IsEqualTo(GitRefKind.Tag);
    }

    // ============ Ref Resolution Tests ============
    // (Tests for GitService.ResolveRef with various ref types)

    /// <summary>
    /// Explicit tag resolution
    /// </summary>
    [Test]
    public async Task ResolveRef_ExplicitTag_ResolvesToTagCommitSha()
    {
        await CreateBranchTagRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        var ref_ = await _gitService.ResolveRefAsync("v1.0.0");

        await Assert.That(ref_.Name).IsEqualTo("v1.0.0");
        await Assert.That(ref_.Kind).IsEqualTo(GitRefKind.Tag);
        await Assert.That(ref_.CommitSha.Length).IsEqualTo(40);
    }

    /// <summary>
    /// Non-existent tag throws
    /// </summary>
    [Test]
    public async Task ResolveRef_NonExistentTag_ThrowsInvalidOperationException()
    {
        await CreateBranchTagRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        await Assert
            .That(async () => await _gitService.ResolveRefAsync("v99.99.99"))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Branch name resolution
    /// </summary>
    [Test]
    public async Task ResolveRef_BranchName_ResolvesToBranchTip()
    {
        await CreateBranchTagRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        var ref_ = await _gitService.ResolveRefAsync("main");

        await Assert.That(ref_.Name).IsEqualTo("main");
        await Assert.That(ref_.Kind).IsEqualTo(GitRefKind.Branch);
        await Assert.That(ref_.CommitSha.Length).IsEqualTo(40);
    }

    /// <summary>
    /// Non-existent branch throws
    /// </summary>
    [Test]
    public async Task ResolveRef_NonExistentBranch_ThrowsInvalidOperationException()
    {
        await CreateBranchTagRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        await Assert
            .That(async () => await _gitService.ResolveRefAsync("not-a-branch"))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// HEAD resolution
    /// </summary>
    [Test]
    public async Task ResolveRef_HEAD_ResolvesToCurrentCommit()
    {
        await CreateBranchTagRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        var ref_ = await _gitService.ResolveRefAsync("HEAD");

        await Assert.That(ref_.Name).IsEqualTo("HEAD");
        await Assert.That(ref_.Kind).IsEqualTo(GitRefKind.Special);
        await Assert.That(ref_.CommitSha.Length).IsEqualTo(40);
    }

    /// <summary>
    /// Full commit SHA resolution
    /// </summary>
    [Test]
    public async Task ResolveRef_FullCommitSha_ResolvesToCommitSha()
    {
        await CreateBranchTagRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        var headRef = await _gitService.ResolveRefAsync("HEAD");
        var fullSha = headRef.CommitSha;

        var ref_ = await _gitService.ResolveRefAsync(fullSha);

        await Assert.That(ref_.Kind).IsEqualTo(GitRefKind.Commit);
        await Assert.That(ref_.CommitSha).IsEqualTo(fullSha);
    }

    /// <summary>
    /// Abbreviated commit SHA resolution
    /// </summary>
    [Test]
    public async Task ResolveRef_AbbreviatedCommitSha_ResolvesToFullSha()
    {
        await CreateBranchTagRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        var headRef = await _gitService.ResolveRefAsync("HEAD");
        var abbrevSha = headRef.CommitSha[..10];

        var ref_ = await _gitService.ResolveRefAsync(abbrevSha);

        await Assert.That(ref_.Kind).IsEqualTo(GitRefKind.Commit);
        await Assert.That(ref_.CommitSha.Length).IsEqualTo(40);
        await Assert.That(ref_.CommitSha[..10]).IsEqualTo(abbrevSha);
    }

    /// <summary>
    /// Invalid commit SHA throws
    /// </summary>
    [Test]
    public async Task ResolveRef_InvalidCommitSha_ThrowsInvalidOperationException()
    {
        await CreateBranchTagRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        await Assert
            .That(async () => await _gitService.ResolveRefAsync("abcdef0123456789"))
            .Throws<InvalidOperationException>();
    }

    /// <summary>
    /// Detached HEAD at tag
    /// </summary>
    [Test]
    public async Task ResolveRef_DetachedHeadAtTag_ResolvesHeadAndTag()
    {
        await CreateBranchTagRepoAsync();
        await GlobalHooks.RunGitAsync("checkout v1.0.0", _testRepoPath);
        _gitService.SetWorkingDirectory(_testRepoPath);

        var headRef = await _gitService.ResolveRefAsync("HEAD");
        var tagRef = await _gitService.ResolveRefAsync("v1.0.0");

        await Assert.That(headRef.Kind).IsEqualTo(GitRefKind.Special);
        await Assert.That(headRef.CommitSha).IsEqualTo(tagRef.CommitSha);
    }

    /// <summary>
    /// Ambiguous ref resolution is consistent
    /// </summary>
    [Test]
    public async Task ResolveRef_SameRefTwice_ReturnsConsistentResult()
    {
        await CreateBranchTagRepoAsync();
        _gitService.SetWorkingDirectory(_testRepoPath);

        var ref1 = await _gitService.ResolveRefAsync("v1.0.0");
        var ref2 = await _gitService.ResolveRefAsync("v1.0.0");

        await Assert.That(ref1.CommitSha).IsEqualTo(ref2.CommitSha);
    }

    // ============ Helper Methods ============

    private async Task CreateMinimalRepoAsync()
    {
        await GlobalHooks.RunGitAsync("init --initial-branch=main", _testRepoPath);
        await GlobalHooks.RunGitAsync("config user.email \"test@example.com\"", _testRepoPath);
        await GlobalHooks.RunGitAsync("config user.name \"Test User\"", _testRepoPath);

        await File.WriteAllTextAsync(Path.Combine(_testRepoPath, "file1.txt"), "Initial content");
        await GlobalHooks.RunGitAsync("add .", _testRepoPath);
        await GlobalHooks.RunGitAsync("commit -m \"Initial commit\"", _testRepoPath);
    }

    private async Task CreateBranchTagRepoAsync()
    {
        await GlobalHooks.RunGitAsync("init --initial-branch=main", _testRepoPath);
        await GlobalHooks.RunGitAsync("config user.email \"test@example.com\"", _testRepoPath);
        await GlobalHooks.RunGitAsync("config user.name \"Test User\"", _testRepoPath);

        // First commit on main
        await File.WriteAllTextAsync(
            Path.Combine(_testRepoPath, "file1.txt"),
            "First commit content"
        );
        await GlobalHooks.RunGitAsync("add .", _testRepoPath);
        await GlobalHooks.RunGitAsync("commit -m \"First commit\"", _testRepoPath);
        await GlobalHooks.RunGitAsync("tag v1.0.0", _testRepoPath);

        // Second commit
        await File.WriteAllTextAsync(
            Path.Combine(_testRepoPath, "file2.txt"),
            "Second commit content"
        );
        await GlobalHooks.RunGitAsync("add .", _testRepoPath);
        await GlobalHooks.RunGitAsync("commit -m \"Second commit\"", _testRepoPath);
        await GlobalHooks.RunGitAsync("tag v2.0.0", _testRepoPath);
    }

    private async Task CreateMergeRepoAsync()
    {
        await GlobalHooks.RunGitAsync("init --initial-branch=main", _testRepoPath);
        await GlobalHooks.RunGitAsync("config user.email \"test@example.com\"", _testRepoPath);
        await GlobalHooks.RunGitAsync("config user.name \"Test User\"", _testRepoPath);

        // Initial commit
        await File.WriteAllTextAsync(Path.Combine(_testRepoPath, "file1.txt"), "Initial");
        await GlobalHooks.RunGitAsync("add .", _testRepoPath);
        await GlobalHooks.RunGitAsync("commit -m \"Initial commit\"", _testRepoPath);
        await GlobalHooks.RunGitAsync("tag v1.0.0", _testRepoPath);

        // Create feature branch
        await GlobalHooks.RunGitAsync("checkout -b feature/test", _testRepoPath);
        await File.WriteAllTextAsync(Path.Combine(_testRepoPath, "feature.txt"), "Feature content");
        await GlobalHooks.RunGitAsync("add .", _testRepoPath);
        await GlobalHooks.RunGitAsync("commit -m \"Feature commit\"", _testRepoPath);

        // Switch back to main and merge (--no-ff forces a merge commit)
        await GlobalHooks.RunGitAsync("checkout main", _testRepoPath);
        await GlobalHooks.RunGitAsync(
            "merge --no-ff feature/test -m \"Merge feature branch\"",
            _testRepoPath
        );
    }
}
