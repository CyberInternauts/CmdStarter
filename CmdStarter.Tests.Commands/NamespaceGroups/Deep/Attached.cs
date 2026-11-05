namespace CyberInternauts.CmdStarter.Tests.Commands.NamespaceGroups.Deep;

[Parent<Host>]
public class Attached : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 47);
}
