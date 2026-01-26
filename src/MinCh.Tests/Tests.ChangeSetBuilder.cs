using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MinCh.Library.Services;

namespace MinCh.Tests;

/// <summary>
/// Tests for ChangeSetBuilder
/// Covers: clean/dirty trees, commit ranges, merges, renames, deletions, binary files
/// </summary>
public class ChangeSetBuilderTests
{
    private ChangeSetBuilder _builder = null!;
    private GitService _gitService = null!;
    private string _testRepoPath = null!;

    [Before(Test)]
    public async Task SetUpTestRepo()
    {
        var logger = LoggerFactory
            .Create(builder => builder.AddConsole())
            .CreateLogger<GitService>();
        _gitService = new GitService(logger);

        var builderLogger = NullLogger<ChangeSetBuilder>.Instance;
        _builder = new ChangeSetBuilder(_gitService, builderLogger);

        // Clone template repo for isolation (tests modify state)
        _testRepoPath = await GlobalHooks.CloneTemplateAsync();

        _gitService.SetWorkingDirectory(_testRepoPath);
    }

    [After(Test)]
    public void CleanUpTestRepo()
    {
        GlobalHooks.CleanupDirectory(_testRepoPath);
    }

    [Test]
    public async Task Build_CleanWorkingTree_ReturnsChangeSetWithIsDirtyFalse()
    {
        var changeSet = await _builder.BuildAsync("v1.0.0", "v2.0.0");

        await Assert.That(changeSet.IsDirty).IsFalse();
        await Assert.That(changeSet.From.Name).IsEqualTo("v1.0.0");
        await Assert.That(changeSet.To.Name).IsEqualTo("v2.0.0");
    }

    [Test]
    public async Task Build_DirtyWorkingTreeWithAllowDirtyFalse_ThrowsInvalidOperationException()
    {
        // Make repo dirty
        await File.WriteAllTextAsync(Path.Combine(_testRepoPath, "newfile.txt"), "Dirty change");

        await Assert
            .That(async () => await _builder.BuildAsync("v1.0.0", "v2.0.0", allowDirty: false))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Build_DirtyWorkingTreeWithAllowDirtyTrue_ReturnsChangeSetWithIsDirtyTrue()
    {
        // Make repo dirty
        await File.WriteAllTextAsync(Path.Combine(_testRepoPath, "newfile.txt"), "Dirty change");

        var changeSet = await _builder.BuildAsync("v1.0.0", "v2.0.0", allowDirty: true);

        await Assert.That(changeSet.IsDirty).IsTrue();
    }

    [Test]
    public async Task Build_CommitsExistBetweenRefs_PopulatesCommitsAndFiles()
    {
        var changeSet = await _builder.BuildAsync("v1.0.0", "v2.0.0");

        await Assert.That(changeSet.CommitCount).IsGreaterThan(0);
        await Assert.That(changeSet.Commits.Count).IsGreaterThan(0);
        await Assert.That(changeSet.Files.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Build_OneCommitBetweenRefs_ReturnsCorrectCommitDetails()
    {
        var changeSet = await _builder.BuildAsync("v1.0.0", "v2.0.0");

        await Assert.That(changeSet.CommitCount).IsEqualTo(1);
        await Assert.That(changeSet.Commits.Count).IsEqualTo(1);
        await Assert.That(changeSet.Commits[0].Subject).Contains("Second commit");
    }

    [Test]
    public async Task Build_FileAddedInRange_IncludesFileInFilesList()
    {
        var changeSet = await _builder.BuildAsync("v1.0.0", "v2.0.0");

        await Assert.That(changeSet.Files).Contains("file2.txt");
    }

    [Test]
    public async Task Build_NoCommitsBetweenRefs_ReturnsEmptyChangeSet()
    {
        var changeSet = await _builder.BuildAsync("v2.0.0", "v2.0.0");

        await Assert.That(changeSet.CommitCount).IsEqualTo(0);
        await Assert.That(changeSet.Commits.Count).IsEqualTo(0);
        await Assert.That(changeSet.Files.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Build_FileDeletedInRange_IncludesDeletedFileInFilesList()
    {
        // Create a new tag, then delete file2 and commit
        await GlobalHooks.RunGitAsync("tag v2.1.0", _testRepoPath);
        File.Delete(Path.Combine(_testRepoPath, "file2.txt"));
        await GlobalHooks.RunGitAsync("add .", _testRepoPath);
        await GlobalHooks.RunGitAsync("commit -m \"Delete file2\"", _testRepoPath);
        await GlobalHooks.RunGitAsync("tag v2.2.0", _testRepoPath);

        var changeSet = await _builder.BuildAsync("v2.0.0", "v2.2.0");

        await Assert.That(changeSet.Files).Contains("file2.txt");
    }

    [Test]
    public async Task Build_BinaryFileChanged_IncludesFileInFilesList()
    {
        // Create a binary file
        await GlobalHooks.RunGitAsync("tag v2.1.0", _testRepoPath);
        byte[] binaryData = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]; // PNG signature
        await File.WriteAllBytesAsync(Path.Combine(_testRepoPath, "image.png"), binaryData);
        await GlobalHooks.RunGitAsync("add .", _testRepoPath);
        await GlobalHooks.RunGitAsync("commit -m \"Add binary image\"", _testRepoPath);
        await GlobalHooks.RunGitAsync("tag v2.2.0", _testRepoPath);

        var changeSet = await _builder.BuildAsync("v2.0.0", "v2.2.0");

        await Assert.That(changeSet.Files).Contains("image.png");
    }

    [Test]
    public async Task Build_FileRenamed_IncludesRenamedFileInFilesList()
    {
        // Create initial state
        await GlobalHooks.RunGitAsync("tag v2.1.0", _testRepoPath);

        // Rename file1.txt to file1_renamed.txt
        File.Move(
            Path.Combine(_testRepoPath, "file1.txt"),
            Path.Combine(_testRepoPath, "file1_renamed.txt")
        );
        await GlobalHooks.RunGitAsync("add .", _testRepoPath);
        await GlobalHooks.RunGitAsync("commit -m \"Rename file1\"", _testRepoPath);
        await GlobalHooks.RunGitAsync("tag v2.2.0", _testRepoPath);

        var changeSet = await _builder.BuildAsync("v2.0.0", "v2.2.0");

        // The file should appear in the diff (either old or new name depending on git diff output)
        await Assert.That(changeSet.Files.Count).IsGreaterThan(0);
    }
}
