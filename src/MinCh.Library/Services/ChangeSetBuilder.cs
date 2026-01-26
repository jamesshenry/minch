using Microsoft.Extensions.Logging;
using MinCh.Library.Git;

namespace MinCh.Library.Services;

public interface IChangeSetBuilder
{
    Task<ChangeSet> BuildAsync(string from, string to = "HEAD", bool allowDirty = false);
}

public class ChangeSetBuilder(IGitService gitService, ILogger<ChangeSetBuilder> logger)
    : IChangeSetBuilder
{
    private readonly IGitService _service = gitService;
    private readonly ILogger<ChangeSetBuilder> _logger = logger;

    public async Task<ChangeSet> BuildAsync(
        string from,
        string to = "HEAD",
        bool allowDirty = false
    )
    {
        // Handle special "last-tag" keyword
        var fromRefName = from;
        if (from.Equals("last-tag", StringComparison.OrdinalIgnoreCase))
        {
            fromRefName =
                await _service.GetLastTagAsync()
                ?? throw new InvalidOperationException("No tags found in repository");
            _logger.LogTrace($"Resolved 'last-tag' to: {fromRefName}");
        }

        var fromRef = await _service.ResolveRefAsync(fromRefName);
        _logger.LogDebug($"From ref resolved to: {fromRef}");
        var toRef = await _service.ResolveRefAsync(to);
        _logger.LogDebug($"To ref resolved to: {toRef}");
        bool dirty = await _service.IsDirtyAsync();
        if (!allowDirty && dirty)
            throw new InvalidOperationException("Working tree is dirty");

        var commits = await _service.GetCommitsAsync(fromRef, toRef);
        var files = await _service.GetFilesAsync(fromRef, toRef);

        return new ChangeSet
        {
            From = fromRef,
            To = toRef,
            IsDirty = dirty,
            CommitCount = commits.Count,
            Commits = commits,
            Files = files,
        };
    }
}
