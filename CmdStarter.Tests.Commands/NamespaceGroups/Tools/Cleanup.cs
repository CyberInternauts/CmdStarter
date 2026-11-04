namespace CyberInternauts.CmdStarter.Tests.Commands.NamespaceGroups.Tools;

public class Cleanup : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 46);
}
