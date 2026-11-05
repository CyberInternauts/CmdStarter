using CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopChildren.Parent;

namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopChildren.Child;

[Parent<LoopParent>]
public class LoopChild : StarterCommand
{
}
