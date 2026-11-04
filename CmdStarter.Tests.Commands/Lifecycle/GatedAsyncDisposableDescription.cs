using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

// Like AsyncDisposableDescription, but its DisposeAsync waits for a signal that the test gives: only an asynchronous wait lets the caller go on before that.
public sealed class GatedAsyncDisposableDescription : StarterCommand, IAsyncDisposable
{
    public static int AsyncDisposals;

    public static TaskCompletionSource Gate { get; set; } = NewGate();

    public override Delegate HandlingMethod => (Func<int>)(() => 0);

    public static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async ValueTask DisposeAsync()
    {
        await Gate.Task;
        Interlocked.Increment(ref AsyncDisposals);
    }
}
