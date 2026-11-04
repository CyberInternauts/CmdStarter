using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.MethodGroups;

[Command("dup path")]
public sealed class DuplicatePathA
{
    [Command]
    public static int Go() => 0;
}

[Command("dup path")]
public sealed class DuplicatePathB
{
    [Command]
    public static int Go() => 0;
}
