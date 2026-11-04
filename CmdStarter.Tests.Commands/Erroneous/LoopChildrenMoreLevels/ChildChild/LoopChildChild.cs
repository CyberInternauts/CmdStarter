using CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopChildrenMoreLevels.Parent;

namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopChildrenMoreLevels.ChildChild;

[Parent<LoopParent>]
public class LoopChildChild : StarterCommand
{
}
