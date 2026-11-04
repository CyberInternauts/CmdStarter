// No namespace on purpose: discovery never finds a command type without a namespace, and the tests give this one to Starter.CommandsTypes.
public sealed class GlobalNamespaceCommand : CyberInternauts.CmdStarter.StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}
