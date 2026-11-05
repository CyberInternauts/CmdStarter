using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Samples.Daemon.Commands;

// The state a daemon keeps from one invocation to the next: a singleton of the process that serves.
public sealed class Counter
{
    public int Value { get; set; }
}

public static class CounterCommands
{
    [Command]
    public static int Count(CommandConsole console, Counter counter)
    {
        counter.Value++;
        console.Out.WriteLine(counter.Value);
        return 0;
    }
}
