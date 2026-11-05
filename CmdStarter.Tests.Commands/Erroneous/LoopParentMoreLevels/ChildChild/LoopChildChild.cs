using CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopParentMoreLevels.Parent;

namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopParentMoreLevels.ChildChild;

[Parent<LoopParent>]
public class LoopChildChild : StarterCommand
{
}
