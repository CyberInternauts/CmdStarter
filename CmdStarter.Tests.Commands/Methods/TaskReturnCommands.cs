namespace CyberInternauts.CmdStarter.Tests.Commands.Methods;

public static class TaskReturnCommands
{
    [Command]
    public static async Task PlainTask()
    {
        await Task.Yield();
        MethodsLog.Entries.Add("plain-task");
    }

    [Command]
    public static async ValueTask PlainValueTask()
    {
        await Task.Yield();
        MethodsLog.Entries.Add("plain-value-task");
    }
}
