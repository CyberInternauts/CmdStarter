namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.NameConflicts.ChildLikeParent.Sub;

[Parent<CyberInternauts.CmdStarter.Tests.Commands.Erroneous.NameConflicts.ChildLikeParent.Get>]
public sealed class Get : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}
