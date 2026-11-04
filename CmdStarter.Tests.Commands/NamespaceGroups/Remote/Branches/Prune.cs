namespace CyberInternauts.CmdStarter.Tests.Commands.NamespaceGroups.Remote.Branches;

public class Prune : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 49);
}
