using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Samples.ReplServer.Commands;

public static class AdminCommands
{
    [Command]
    public static int Status(CommandConsole console)
    {
        console.Out.WriteLine("running");
        return 0;
    }

    [Command]
    public static int Echo(CommandConsole console, string text)
    {
        console.Out.WriteLine(text);
        return 0;
    }
}
