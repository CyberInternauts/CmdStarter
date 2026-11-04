namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.NameConflicts.ChildLikeParent;

public sealed class Get : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}
