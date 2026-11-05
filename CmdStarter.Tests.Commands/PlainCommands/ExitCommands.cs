namespace CyberInternauts.CmdStarter.Tests.Commands.PlainCommands;

[Command]
public sealed class ExitInt
{
    public int Run() => 7;
}

[Command]
public sealed class ExitTask
{
    public async Task Run()
    {
        await Task.Yield();
        PlainLog.Entries.Add("task");
    }
}

[Command]
public sealed class ExitTaskInt
{
    public async Task<int> Run()
    {
        await Task.Yield();
        return 9;
    }
}

[Command]
public sealed class ExitVoid
{
    public void Run() => PlainLog.Entries.Add("void");
}
