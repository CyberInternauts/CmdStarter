using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Tests.Commands.Consoles;

public sealed class Write : StarterCommand
{
    public override Delegate HandlingMethod => (Func<CommandConsole, int>)(console =>
    {
        console.Out.WriteLine("class out");
        console.Error.WriteLine("class err");
        return 0;
    });
}

public sealed class ReadLength : StarterCommand
{
    public override Delegate HandlingMethod => (Func<CommandConsole, int>)(console => console.In.ReadLine()?.Length ?? -1);
}

public sealed class Throw : StarterCommand
{
    public override Delegate HandlingMethod => (Action)(() => throw new InvalidOperationException("console failure"));
}

public static class ConsoleMethods
{
    [Command]
    public static int Echo(CommandConsole console, string text)
    {
        console.Out.WriteLine(text);
        return 0;
    }
}

/// <summary>
/// Waits until the expected number of executions have started before it writes, so that executions overlap deterministically.
/// </summary>
public sealed class Rendezvous : StarterCommand
{
    private static int started;
    private static int expected;
    private static TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static void Reset(int expectedExecutions)
    {
        expected = expectedExecutions;
        started = 0;
        release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public override Delegate HandlingMethod => (Func<CommandConsole, string, Task<int>>)(async (console, name) =>
    {
        if (Interlocked.Increment(ref started) == expected)
        {
            release.TrySetResult();
        }

        await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
        console.Out.WriteLine($"hello {name}");
        return 0;
    });
}

/// <summary>
/// Starts "write" on the same starter with another console, then writes to its own console.
/// </summary>
public sealed class NestedStart : StarterCommand
{
    public static Starter? CurrentStarter;

    public static CommandConsole? InnerConsole;

    public override Delegate HandlingMethod => (Func<CommandConsole, Task<int>>)(async console =>
    {
        await CurrentStarter!.Start("write", InnerConsole!);
        console.Out.WriteLine("after nested");
        return 0;
    });
}
