using TUnit.Core.Interfaces;

namespace MinCh.Tests;

public class DataClass : IAsyncInitializer, IAsyncDisposable
{
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
