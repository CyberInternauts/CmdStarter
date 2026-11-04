namespace CyberInternauts.CmdStarter.Tests.Commands.Methods.Erroneous;

public sealed class BlankNameCommands
{
    [Command("   ")]
    public static int Run() => 0;
}
