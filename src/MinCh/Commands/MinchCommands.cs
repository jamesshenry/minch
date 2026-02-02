using ConsoleAppFramework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using MinCh.Configuration;
using MinCh.Infrastructure;
using MinCh.Library.Git;
using MinCh.Library.Services;
using MinCh.Services;
using Spectre.Console;
using Spectre.Console.Json;

namespace MinCh.Commands;

[RegisterCommands]
public class MinchCommands(
    IChangeSetBuilder builder,
    ILogger<MinchCommands> logger,
    IOptions<AppConfig> options,
    RendererFactory factory
)
{
    private readonly AppConfig _config = options.Value;
    private readonly IChangeSetBuilder _builder = builder;
    private readonly ILogger<MinchCommands> _logger = logger;
    private readonly RendererFactory _factory = factory;

    /// <summary>
    /// Computes the set of changes between two Git references.
    /// </summary>
    /// <param name="from">Explicit baseline (overrides positional baseline)</param>
    /// <param name="to">Target ref to compare against</param>
    /// <param name="format"> Output format: text | json</param>
    /// <param name="check"></param>
    [Command("")]
    public async Task Root(
        string from = "last-tag",
        string to = "HEAD",
        string format = "text",
        CheckMode check = CheckMode.Dirty
    )
    {
        try
        {
            var changeSet = await _builder.BuildAsync(from, to, check == CheckMode.Dirty);

            var renderer = _factory.GetRenderer(format);
            var rendered = renderer.Render(changeSet);
            if (renderer is JsonRenderer)
            {
                var jsonText = new JsonText(rendered);
                AnsiConsole.Write(jsonText);
            }
            else
            {
                AnsiConsole.Write(rendered);
            }
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{ex.Message}");
            Environment.Exit(2);
        }
    }

    [Command("init")]
    public async Task Init()
    {
        await StartupTasks.InitializeAsync(_logger);
    }

    [Command("generate")]
    public async Task Generate(
        [FromServices] IChangelogGenerator generator,
        [FromServices] IGitService git,
        string version,
        string from = "last-tag",
        string to = "HEAD",
        CheckMode check = CheckMode.Dirty,
        string? output = null
    )
    {
        try
        {
            var changeSet = await _builder.BuildAsync(from, to, check == CheckMode.Dirty);

            var root = await git.GetRepoRootAsync();

            var changelogFile = Path.Combine(root, "CHANGELOG.md");
            await generator.GenerateAsync(changeSet, versio`n, changelogFile);
        }
        catch
        {
            throw;
        }
    }
}

public enum CheckMode
{
    None,
    Dirty,
}

public interface IChangelogGenerator
{
    Task GenerateAsync(ChangeSet changeSet, string version, string output);
}

public class KeepAChangelogGenerator(RendererFactory factory) : IChangelogGenerator
{
    private readonly RendererFactory _factory = factory; // Reuse for KeepAChangelog rendering

    public async Task GenerateAsync(ChangeSet change`Set, string version, string outputPath)
    {
        var rendered = _factory.GetRenderer("keepachangelog").Render(changeSet);

        // Now the tricky part: merge with existing file
        var existing = File.Exists(outputPath) ? File.ReadAllText(outputPath) : null;
        var merged = MergeChangelog(existing, rendered, version);

        File.WriteAllText(outputPath, merged);
    }

    private string MergeChangelog(string? existing, string rendered, string version)
    {
        // Version detection, conflict checking, insertion logic
        throw new NotImplementedException();
    }
}
