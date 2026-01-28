using System.Runtime.CompilerServices;
using ConsoleAppFramework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinCh.Configuration;
using MinCh.Infrastructure;
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
    /// <param name="to">Target ref to compare against (default: HEAD)</param>
    /// <param name="output"> Output format: text | json</param>
    /// <param name="check"></param>
    [Command("")]
    public async Task Root(
        string from = "last-tag",
        string to = "HEAD",
        string output = "text",
        CheckMode check = CheckMode.Dirty
    )
    {
        try
        {
            var changeSet = await _builder.BuildAsync(from, to, check == CheckMode.Dirty);

            var renderer = _factory.GetRenderer(output);
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
            Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
            Environment.Exit(2);
        }
    }

    [Command("init")]
    public async Task Init()
    {
        await StartupTasks.InitializeAsync(_logger);
    }
}

public enum CheckMode
{
    None,
    Dirty,
}
