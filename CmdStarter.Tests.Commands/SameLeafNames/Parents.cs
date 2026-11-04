namespace CyberInternauts.CmdStarter.Tests.Commands.SameLeafNames;

public sealed class Users : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}

public sealed class Groups : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}
