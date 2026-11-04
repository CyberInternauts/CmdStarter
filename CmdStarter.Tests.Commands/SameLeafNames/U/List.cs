namespace CyberInternauts.CmdStarter.Tests.Commands.SameLeafNames.U;

[Parent<CyberInternauts.CmdStarter.Tests.Commands.SameLeafNames.Users>]
public sealed class List : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 21);
}
