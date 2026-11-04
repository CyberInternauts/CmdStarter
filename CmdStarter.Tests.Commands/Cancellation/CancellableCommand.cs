namespace CyberInternauts.CmdStarter.Tests.Commands.Cancellation;

public sealed class CancellableCommand : StarterCommand
{
    public static TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override Delegate HandlingMethod => (Func<CancellationToken, Task<int>>)(async token =>
    {
        Started.TrySetResult();
        await Task.Delay(Timeout.Infinite, token);
        return 0;
    });

    public static void Reset()
    {
        Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
