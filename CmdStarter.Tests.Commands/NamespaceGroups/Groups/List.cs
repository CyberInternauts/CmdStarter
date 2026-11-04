namespace CyberInternauts.CmdStarter.Tests.Commands.NamespaceGroups.Groups;

public class List : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 44);
}
