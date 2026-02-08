using ConsoleAppFramework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinCh.Configuration;
using MinCh.Infrastructure;
using MinCh.Library.Changelog;
using MinCh.Library.Rendering;
using MinCh.Library.Services;
using Spectre.Console;
using Spectre.Console.Json;

namespace MinCh.Commands;

[RegisterCommands]
public class MinchCommands(
    IChangeSetBuilder builder,
    ILogger<MinchCommands> logger,
    IOptions<AppConfig> options
)
{
    private readonly AppConfig _config = options.Value;
    private readonly IChangeSetBuilder _builder = builder;
    private readonly ILogger<MinchCommands> _logger = logger;

    /// <summary>
    /// Computes the set of changes between two Git references.
    /// </summary>
    /// <param name="from">Explicit baseline (overrides positional baseline)</param>
    /// <param name="to">Target ref to compare against</param>
    /// <param name="format"> Output format: text | json</param>
    /// <param name="check"></param>
    [Command("")]
    public async Task Root(
        [FromServices] ChangeSetRendererFactory factory,
        string from = "last-tag",
        string to = "HEAD",
        string format = "text",
        CheckMode check = CheckMode.Dirty
    )
    {
        try
        {
            var changeSet = await _builder.BuildAsync(from, to, check == CheckMode.Dirty);

            var renderer = factory.Get(format);
            var rendered = renderer.Render(changeSet);
            if (renderer is JsonChangeSetRenderer)
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
        [FromServices] ChangelogRendererFactory factory,
        [FromServices] IGitService git,
        string version,
        CheckMode check = CheckMode.Dirty,
        string? output = null
    )
    {
        var changeSet = await _builder.BuildAsync("last-tag", "HEAD", check == CheckMode.Dirty);
        var changelog = await generator.Generate(changeSet, version);

        var renderer = factory.Get("markdown");
        string section = renderer.Render(changelog);

        Console.WriteLine(section);
    }
}

public enum CheckMode
{
    None,
    Dirty,
}
