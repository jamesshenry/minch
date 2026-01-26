using Microsoft.Extensions.Logging;
using MinCh.Library.Git;
using MinCh.Library.Services;

namespace MinCh.Commands;

public interface IChangeSetBuilder
{
    ChangeSet Build(string from, string to = "HEAD", bool allowDirty = false);
}

public class ChangeSetBuilder(IGitService gitService, ILogger<ChangeSetBuilder> logger)
    : IChangeSetBuilder
{
    private readonly IGitService _service = gitService;
    private readonly ILogger<ChangeSetBuilder> _logger = logger;

    public ChangeSet Build(string from, string to = "HEAD", bool allowDirty = false)
    {
        // Handle special "last-tag" keyword
        var fromRefName = from;
        if (from.Equals("last-tag", StringComparison.OrdinalIgnoreCase))
        {
            fromRefName =
                _service.GetLastTag()
                ?? throw new InvalidOperationException("No tags found in repository");
            _logger.LogDebug($"Resolved 'last-tag' to: {fromRefName}");
        }

        var fromRef = _service.ResolveRef(fromRefName);
        _logger.LogDebug($"From ref resolved to: {fromRef}");
        var toRef = _service.ResolveRef(to);
        _logger.LogDebug($"To ref resolved to: {toRef}");

        if (!allowDirty && _service.IsDirty())
            throw new InvalidOperationException("Working tree is dirty");

        var commits = _service.GetCommits(fromRef, toRef);
        var files = _service.GetFiles(fromRef, toRef);

        return new ChangeSet
        {
            From = fromRef,
            To = toRef,
            IsDirty = _service.IsDirty(),
            CommitCount = commits.Count,
            Commits = commits,
            Files = files,
        };
    }
}
