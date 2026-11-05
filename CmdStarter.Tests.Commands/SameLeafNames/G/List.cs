namespace CyberInternauts.CmdStarter.Tests.Commands.SameLeafNames.G;

[Parent<CyberInternauts.CmdStarter.Tests.Commands.SameLeafNames.Groups>]
public sealed class List : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 22);
}
