namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Lifecycle;

public sealed class AsyncScopedService : IAsyncDisposable
{
    private bool isDisposed;
    public bool IsDisposed => isDisposed;
    public ValueTask DisposeAsync() { isDisposed = true; return ValueTask.CompletedTask; }
}
