using MinCh.Library.Git;

namespace MinCh.Library.Services;

public interface IGitService
{
    Task<IReadOnlyList<Commit>> GetCommitsAsync(Ref fromRef, Ref toRef);
    Task<IReadOnlyList<string>> GetFilesAsync(Ref fromRef, Ref toRef);
    Task<string?> GetLastTagAsync();
    Task<string> GetRepoRootAsync();
    Task<bool> IsDirtyAsync();
    Task<Ref> ResolveRefAsync(string refName);
    void SetWorkingDirectory(string path);
}

public interface IChangelogGenerator
{
    Task<Changelog> GenerateAsync(ChangeSet changeSet, string version);
}

public class ChangelogGenerator(RendererFactory factory) : IChangelogGenerator
{
    private readonly RendererFactory _factory = factory; // Reuse for KeepAChangelog rendering

    public async Task<Changelog> GenerateAsync(ChangeSet changeSet, string version)
    {
        var renderer = _factory.GetRenderer("markdown");
        var rendered = renderer.Render(changeSet);

        return rendered;
    }
}
