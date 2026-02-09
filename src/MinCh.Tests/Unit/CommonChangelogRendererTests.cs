using MinCh.Library.Changelog;
using MinCh.Library.Git;

namespace MinCh.Library.Tests.Rendering;

public class CommonChangelogRendererTests
{
    private readonly CommonChangelogRenderer _renderer;

    public CommonChangelogRendererTests()
    {
        _renderer = new CommonChangelogRenderer();
    }

    [Test]
    public async Task Render_ShouldGenerateMarkdownForEmptyChangelog()
    {
        // Arrange
        var changelog = new Release("1.0.0", ReleaseDate.On(2026, 2, 6), []);

        // Act
        var output = _renderer.Render(changelog);

        // Assert
        await Assert.That(output).IsEqualTo("## 1.0.0 - 2026-02-06\r\n\r\n");
    }

    [Test]
    public async Task Render_ShouldGenerateMarkdownForChangelogWithGroupsAndItems()
    {
        // Arrange
        var changeItem1 = new ParsedCommit()
        {
            Description = "Initial commit",
            Type = ConventionalCommitType.Feat,
            IsBreaking = false,
        };
        var changeItem2 = new ParsedCommit()
        {
            Description = "Bugfix for login",
            Type = ConventionalCommitType.Fix,
            IsBreaking = false,
        };
        var changelog = new Release(
            "1.0.0",
            ReleaseDate.On(2026, 2, 6),
            [changeItem1, changeItem2]
        );

        const string expectedOutput = """
## 1.0.0 - 2026-02-06

### Features

- feat: Initial commit

### Bug Fixes

- fix: Bugfix for login
""";
        // Act
        var output = _renderer.Render(changelog);

        // Assert
        await Assert.That(output).IsEqualTo(expectedOutput);
    }
}
