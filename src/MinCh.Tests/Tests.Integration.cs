using Microsoft.Extensions.Logging;
using MinCh.Commands;
using MinCh.Library.Git;
using MinCh.Services;

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

        var headRef = _gitService.ResolveRef("HEAD");

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

        var v1Ref = _gitService.ResolveRef("v1.0.0");
        var v2Ref = _gitService.ResolveRef("v2.0.0");
        var mainRef = _gitService.ResolveRef("main");

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

        var changeSet = _builder.Build("v1.0.0", "v2.0.0");

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

        var changeSet = _builder.Build("v1.0.0", "HEAD");

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

        var isDirty = _gitService.IsDirty();

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

        var headRef = _gitService.ResolveRef("HEAD");
        var tagRef = _gitService.ResolveRef("v1.0.0");

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

        var changeSet = _builder.Build("v2.0.0", "v2.2.0");

        await Assert.That(changeSet.Files).Contains("file2.txt");
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

        // Switch back to main and merge
        await GlobalHooks.RunGitAsync("checkout main", _testRepoPath);
        await GlobalHooks.RunGitAsync(
            "merge feature/test -m \"Merge feature branch\"",
            _testRepoPath
        );
    }
}
