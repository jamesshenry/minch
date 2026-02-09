using MinCh.Library.Git;

namespace MinCh.Library.Git;

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
