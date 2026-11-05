namespace CyberInternauts.CmdStarter.Tests.Commands.NamespaceGroups.Users;

public class List : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 43);
}
