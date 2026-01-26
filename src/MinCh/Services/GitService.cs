using Microsoft.Extensions.Logging;
using MinCh.Library.Git;

namespace MinCh.Services;

public class GitService(ILogger<GitService> logger) : IGitService
{
    public void DoSomething()
    {
        logger.LogCritical("I am doing something");
    }

    public ChangeSet GetChangeSet(Ref fromRef, Ref toRef)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<Commit> GetCommits(Ref fromRef, Ref toRef)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<string> GetFiles(Ref fromRef, Ref toRef)
    {
        throw new NotImplementedException();
    }

    public bool IsDirty()
    {
        throw new NotImplementedException();
    }

    public Ref ResolveRef(string from)
    {
        throw new NotImplementedException();
    }
}
