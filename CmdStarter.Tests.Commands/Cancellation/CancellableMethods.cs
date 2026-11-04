using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Cancellation;

public sealed class CancellableMethods
{
    public static TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static void Reset()
    {
        Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [Command]
    public static async Task<int> WaitForCancel(CancellationToken token)
    {
        Started.TrySetResult();
        await Task.Delay(Timeout.Infinite, token);
        return 0;
    }
}
