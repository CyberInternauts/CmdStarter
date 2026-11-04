namespace CyberInternauts.CmdStarter.Tests.Commands.TreeBuilding.EmptyChildren;

// The attribute names no namespace: if it did, it would match every command, this one's sibling included.
[Children("")]
public class EmptyChildrenParent : StarterCommand
{
}
