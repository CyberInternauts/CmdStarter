namespace com.cyberinternauts.csharp.CmdStarter.Samples.Repl.Services;

public sealed class CommandScope
{
    private static int created;

    public int Number { get; } = Interlocked.Increment(ref created);
}
