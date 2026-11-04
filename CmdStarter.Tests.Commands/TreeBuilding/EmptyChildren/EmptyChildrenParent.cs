namespace CyberInternauts.CmdStarter.Tests.Commands.TreeBuilding.EmptyChildren;

// Neither attribute names a namespace: if one did, it would match every command, this one's sibling included.
[Children("")]
[Children((string)null!)]
public class EmptyChildrenParent : StarterCommand
{
}
