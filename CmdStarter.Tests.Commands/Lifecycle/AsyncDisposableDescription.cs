namespace CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

// Only IAsyncDisposable, and it really yields: releasing it synchronously has to block on DisposeAsync.
public sealed class AsyncDisposableDescription : StarterCommand, IAsyncDisposable
{
    public static int AsyncDisposals;

    public override Delegate HandlingMethod => (Func<int>)(() => 0);

    public async ValueTask DisposeAsync()
    {
        await Task.Yield();
        Interlocked.Increment(ref AsyncDisposals);
    }
}
