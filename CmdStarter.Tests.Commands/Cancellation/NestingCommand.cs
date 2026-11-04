namespace CyberInternauts.CmdStarter.Tests.Commands.Cancellation;

// Runs a nested Start (set by the test in Inner) before it signals Started and waits for its cancellation.
public sealed class NestingCommand : StarterCommand
{
    public static TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static Func<Task<int>>? Inner;

    public override Delegate HandlingMethod => (Func<CancellationToken, Task<int>>)(async token =>
    {
        if (Inner is not null)
        {
            await Inner();
        }

        Started.TrySetResult();
        await Task.Delay(Timeout.Infinite, token);
        return 0;
    });

    public static void Reset()
    {
        Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Inner = null;
    }
}
