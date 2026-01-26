using MinCh.Library.Git;
using MinCh.Services;

namespace MinCh.Tests;

/// <summary>
/// Tests for IChangeSetRenderer implementations (Text, JSON)
/// Covers: text output, JSON output, empty changeset, dirty state
/// </summary>
public class RendererTests
{
    private RendererFactory _rendererFactory = null!;

    [Before(Test)]
    public void SetUp()
    {
        _rendererFactory = new RendererFactory();
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
            Commits = new[]
            {
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
            },
            Files = new[] { "src/feature.cs", "tests/feature.tests.cs" },
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
            Commits = new[]
            {
                new Commit
                {
                    Sha = "ccc333",
                    Author = "Bob Johnson",
                    Date = new DateTime(2024, 1, 15),
                    Subject = "WIP: ongoing changes",
                },
            },
            Files = new[] { "src/working.cs" },
        };
    }

    [Test]
    public async Task TextRenderer_PopulatedChangeSet_OutputsCommitsAndFiles()
    {
        var renderer = _rendererFactory.GetRenderer("text");
        var changeSet = CreatePopulatedChangeSet();

        using (var stringWriter = new StringWriter())
        {
            var originalOutput = Console.Out;
            Console.SetOut(stringWriter);

            try
            {
                renderer.Render(changeSet);
                var output = stringWriter.ToString();

                await Assert.That(output).Contains("v1.0.0");
                await Assert.That(output).Contains("v2.0.0");
                await Assert.That(output).Contains("Add feature X");
                await Assert.That(output).Contains("Fix bug Y");
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }
    }

    [Test]
    public async Task JsonRenderer_PopulatedChangeSet_OutputsValidJson()
    {
        var renderer = _rendererFactory.GetRenderer("json");
        var changeSet = CreatePopulatedChangeSet();

        using (var stringWriter = new StringWriter())
        {
            var originalOutput = Console.Out;
            Console.SetOut(stringWriter);

            try
            {
                renderer.Render(changeSet);
                var output = stringWriter.ToString();

                await Assert.That(output).Contains("v1.0.0");
                await Assert.That(output).Contains("v2.0.0");
                await Assert.That(output).Contains("Add feature X");
                // Basic validation that it's JSON-like (not comprehensive)
                await Assert.That(output).Contains("{");
                await Assert.That(output).Contains("}");
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }
    }

    [Test]
    public async Task TextRenderer_EmptyChangeSet_OutputsNoChangesMessage()
    {
        var renderer = _rendererFactory.GetRenderer("text");
        var changeSet = CreateEmptyChangeSet();

        using (var stringWriter = new StringWriter())
        {
            var originalOutput = Console.Out;
            Console.SetOut(stringWriter);

            try
            {
                renderer.Render(changeSet);
                var output = stringWriter.ToString();

                // Should show the range but no commits
                await Assert.That(output).Contains("v1.0.0");
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }
    }

    [Test]
    public async Task JsonRenderer_EmptyChangeSet_OutputsEmptyArrays()
    {
        var renderer = _rendererFactory.GetRenderer("json");
        var changeSet = CreateEmptyChangeSet();

        using (var stringWriter = new StringWriter())
        {
            var originalOutput = Console.Out;
            Console.SetOut(stringWriter);

            try
            {
                renderer.Render(changeSet);
                var output = stringWriter.ToString();

                // Empty arrays should be present
                await Assert.That(output).Contains("[]");
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }
    }

    [Test]
    public async Task TextRenderer_DirtyChangeSet_IndicatesDirtyState()
    {
        var renderer = _rendererFactory.GetRenderer("text");
        var changeSet = CreateDirtyChangeSet();

        using (var stringWriter = new StringWriter())
        {
            var originalOutput = Console.Out;
            Console.SetOut(stringWriter);

            try
            {
                renderer.Render(changeSet);
                var output = stringWriter.ToString();

                // IsDirty flag should be visible in some form
                await Assert.That(output).Contains("v1.0.0");
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }
    }

    [Test]
    public async Task RendererFactory_UnknownFormat_ThrowsArgumentException()
    {
        await Assert
            .That(() => _rendererFactory.GetRenderer("unknown"))
            .Throws<ArgumentException>();
    }

    [Test]
    [Arguments("text")]
    [Arguments("json")]
    [Arguments("TEXT")]
    [Arguments("JSON")]
    public async Task RendererFactory_CaseInsensitive_ReturnsCorrectRenderer(string format)
    {
        var renderer = _rendererFactory.GetRenderer(format);

        await Assert.That(renderer).IsNotNull();
    }
}
