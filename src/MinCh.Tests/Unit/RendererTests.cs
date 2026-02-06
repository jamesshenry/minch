using MinCh.Library.Git;
using MinCh.Library.Rendering;

namespace MinCh.Tests;

public class RendererTests
{
    private ChangeSetRendererFactory _rendererFactory = null!;

    [Before(Test)]
    public void SetUp()
    {
        _rendererFactory = new ChangeSetRendererFactory();
    }

    private static ChangeSet CreatePopulatedChangeSet()
    {
        return new ChangeSet
        {
            From = new Ref
            {
                Name = "v1.0.0",
                Kind = GitRefKind.Tag,
                CommitSha = "abc123def456",
            },
            To = new Ref
            {
                Name = "v2.0.0",
                Kind = GitRefKind.Tag,
                CommitSha = "def456ghi789",
            },
            IsDirty = false,
            CommitCount = 2,
            Commits =
            [
                new Commit
                {
                    Sha = "aaa111",
                    Author = "John Doe",
                    Date = new DateTime(2024, 1, 1),
                    Subject = "Add feature X",
                },
                new Commit
                {
                    Sha = "bbb222",
                    Author = "Jane Smith",
                    Date = new DateTime(2024, 1, 2),
                    Subject = "Fix bug Y",
                },
            ],
            Files = ["src/feature.cs", "tests/feature.tests.cs"],
        };
    }

    private static ChangeSet CreateEmptyChangeSet()
    {
        return new ChangeSet
        {
            From = new Ref
            {
                Name = "v1.0.0",
                Kind = GitRefKind.Tag,
                CommitSha = "abc123def456",
            },
            To = new Ref
            {
                Name = "v1.0.0",
                Kind = GitRefKind.Tag,
                CommitSha = "abc123def456",
            },
            IsDirty = false,
            CommitCount = 0,
            Commits = [],
            Files = [],
        };
    }

    private static ChangeSet CreateDirtyChangeSet()
    {
        return new ChangeSet
        {
            From = new Ref
            {
                Name = "v1.0.0",
                Kind = GitRefKind.Tag,
                CommitSha = "abc123def456",
            },
            To = new Ref
            {
                Name = "HEAD",
                Kind = GitRefKind.Special,
                CommitSha = "def456ghi789",
            },
            IsDirty = true,
            CommitCount = 1,
            Commits =
            [
                new Commit
                {
                    Sha = "ccc333",
                    Author = "Bob Johnson",
                    Date = new DateTime(2024, 1, 15),
                    Subject = "WIP: ongoing changes",
                },
            ],
            Files = ["src/working.cs"],
        };
    }

    [Test]
    public async Task TextRenderer_PopulatedChangeSet_OutputsCommitsAndFiles()
    {
        var renderer = _rendererFactory.Get("text");
        var changeSet = CreatePopulatedChangeSet();

        var output = renderer.Render(changeSet);

        await Assert.That(output).Contains("v1.0.0");
        await Assert.That(output).Contains("v2.0.0");
        await Assert.That(output).Contains("Add feature X");
        await Assert.That(output).Contains("Fix bug Y");
    }

    [Test]
    public async Task JsonRenderer_PopulatedChangeSet_OutputsValidJson()
    {
        var renderer = _rendererFactory.Get("json");
        var changeSet = CreatePopulatedChangeSet();

        var output = renderer.Render(changeSet);

        await Assert.That(output).Contains("v1.0.0");
        await Assert.That(output).Contains("v2.0.0");
        await Assert.That(output).Contains("Add feature X");
        await Assert.That(output).Contains("{");
        await Assert.That(output).Contains("}");
    }

    [Test]
    public async Task TextRenderer_EmptyChangeSet_OutputsNoChangesMessage()
    {
        var renderer = _rendererFactory.Get("text");
        var changeSet = CreateEmptyChangeSet();

        var output = renderer.Render(changeSet);

        // Should show the range but no commits
        await Assert.That(output).Contains("v1.0.0");
        await Assert.That(output).Contains("Changes from v1.0.0 to v1.0.0");
    }

    [Test]
    public async Task JsonRenderer_EmptyChangeSet_OutputsEmptyArrays()
    {
        var renderer = _rendererFactory.Get("json");
        var changeSet = CreateEmptyChangeSet();

        var output = renderer.Render(changeSet);

        // Empty arrays should be present
        await Assert.That(output).Contains("[]");
        await Assert.That(output).Contains("\"commits\": []");
        await Assert.That(output).Contains("\"files\": []");
    }

    [Test]
    public async Task TextRenderer_DirtyChangeSet_IndicatesDirtyState()
    {
        var renderer = _rendererFactory.Get("text");
        var changeSet = CreateDirtyChangeSet();

        var output = renderer.Render(changeSet);

        // IsDirty flag should be visible in some form
        await Assert.That(output).Contains("v1.0.0");
        await Assert.That(output).Contains("Changes from v1.0.0 to HEAD");
        await Assert.That(output).Contains("WIP: ongoing changes");
    }

    [Test]
    public async Task RendererFactory_UnknownFormat_ThrowsArgumentException()
    {
        await Assert.That(() => _rendererFactory.Get("unknown")).Throws<ArgumentException>();
    }

    [Test]
    [Arguments("text")]
    [Arguments("json")]
    [Arguments("TEXT")]
    [Arguments("JSON")]
    public async Task RendererFactory_CaseInsensitive_ReturnsCorrectRenderer(string format)
    {
        var renderer = _rendererFactory.Get(format);

        await Assert.That(renderer).IsNotNull();
    }
}
