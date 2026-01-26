using ConsoleAppFramework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinCh.Configuration;
using MinCh.Services;

namespace MinCh.Commands;

public class MinchCommands(
    IChangeSetBuilder builder,
    ILogger<MinchCommands> logger,
    IOptions<AppConfig> options,
    RendererFactory factory
)
{
    private readonly AppConfig config = options.Value;
    private readonly IChangeSetBuilder _builder = builder;
    private readonly RendererFactory _factory = factory;

    /// <summary>
    ///
    /// </summary>
    /// <param name="from">Explicit baseline (overrides positional baseline)</param>
    /// <param name="to">Target ref to compare against (default: HEAD)</param>
    /// <param name="output"> Output format: text | json</param>
    /// <param name="allowDirty"></param>
    [Command("")]
    public void Root(
        string from = "last-tag",
        string to = "HEAD",
        string output = "text",
        bool allowDirty = false
    )
    {
        var changeSet = _builder.Build(from, to, allowDirty);

        var renderer = _factory.GetRenderer(output);
    }
}
