namespace CyberInternauts.CmdStarter.Tests.Commands.Catalog;

// Declared by the hand-written catalog, but abstract: the scan never finds an abstract command, so the engine ignores it.
public abstract class AbstractListed : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}
