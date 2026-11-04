using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Tests.Commands.NamespaceGroups.Methods;

public static class NamespaceGroupMethods
{
    [Command("users export")]
    public static int Export() => 48;
}
