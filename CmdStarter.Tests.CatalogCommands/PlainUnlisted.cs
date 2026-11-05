using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Tests.Commands.Catalog;

// A plain command that the scan would find and that the hand-written catalog leaves out.
[Command]
public sealed class PlainUnlisted
{
    public int Run() => 0;
}
