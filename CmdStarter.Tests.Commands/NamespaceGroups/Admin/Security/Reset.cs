namespace CyberInternauts.CmdStarter.Tests.Commands.NamespaceGroups.Admin.Security;

public class Reset : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 45);
}
