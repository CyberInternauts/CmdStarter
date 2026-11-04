namespace CyberInternauts.CmdStarter.Tests.Commands.Catalog;

// A command that the scan would find and that the hand-written catalog leaves out: the engine must follow the catalog.
public sealed class Unlisted : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}
