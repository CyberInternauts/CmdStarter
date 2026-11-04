using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Tests.Commands.FluentTree.Methods;

public static class FluentMethodCommands
{
    [Command("users export")]
    public static int Export() => 35;
}
