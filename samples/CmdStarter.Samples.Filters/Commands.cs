using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Samples.Filters;

public static class Commands
{
    [Command]
    public static void Status()
    {
        Console.Out.WriteLine("ok");
    }

    [Command]
    [RequiresConfirmation]
    public static void Deploy()
    {
        Console.Out.WriteLine("deployed");
    }
}
