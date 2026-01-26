using MinCh.Library.Git;

namespace MinCh.Library.Services;

public interface IGitService
{
    Task<IReadOnlyList<Commit>> GetCommitsAsync(Ref fromRef, Ref toRef);
    Task<IReadOnlyList<string>> GetFilesAsync(Ref fromRef, Ref toRef);
    Task<string?> GetLastTagAsync();
    Task<bool> IsDirtyAsync();
    Task<Ref> ResolveRefAsync(string refName);
    void SetWorkingDirectory(string path);
}
