using MinCh.Library.Changelog;

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
        var changelog = new ChangelogRecord("1.0.0", ChangelogDate.On(2026, 2, 6), []);

        // Act
        var output = _renderer.Render(changelog);

        // Assert
        await Assert.That(output).IsEqualTo("# Changelog 1.0.0 (2026-02-06)\r\n\r\n");
    }

    [Test]
    public async Task Render_ShouldGenerateMarkdownForChangelogWithGroupsAndItems()
    {
        // Arrange
        var changeItem1 = new ChangeItem("feat: Initial commit", null);
        var changeItem2 = new ChangeItem("fix: Bugfix for login", null);
        var changeGroup1 = new ChangeGroup("Features", new List<ChangeItem> { changeItem1 });
        var changeGroup2 = new ChangeGroup("Bug Fixes", new List<ChangeItem> { changeItem2 });
        var changelog = new ChangelogRecord(
            "1.0.0",
            ChangelogDate.On(2026, 2, 6),
            [changeGroup1, changeGroup2]
        );

        const string expectedOutput = """
# Changelog 1.0.0 (2026-02-06)

## Features

* feat: Initial commit

## Bug Fixes

* fix: Bugfix for login
""";
        // Act
        var output = _renderer.Render(changelog);

        // Assert
        await Assert.That(output).IsEqualTo(expectedOutput);
    }
}
