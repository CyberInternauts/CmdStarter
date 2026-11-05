namespace CyberInternauts.CmdStarter.Tests.Commands.PlainCommands;

// A disposable plain command: DisposeAsync is not its handler, Run is.
[Command]
public sealed class AsyncDisposablePlain : IAsyncDisposable
{
    public int Run()
    {
        PlainLog.Entries.Add("run");
        return 91;
    }

    public ValueTask DisposeAsync()
    {
        PlainLog.Entries.Add("dispose-async");
        return ValueTask.CompletedTask;
    }
}

// A record: the compiler generates public methods (equality, cloning, ToString) that are not the handler either.
[Command]
public sealed record RecordPlain
{
    public int Run() => 92;
}
