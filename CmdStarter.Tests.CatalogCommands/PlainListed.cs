using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Tests.Commands.Catalog;

// A plain command that the hand-written catalog declares with AddCommand.
[Command]
public sealed class PlainListed
{
    public int Run() => 0;
}
