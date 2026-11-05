using CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopChildrenMoreLevels.Child;

namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopChildrenMoreLevels.Parent;

[Parent<LoopChild>]
public class LoopParent : StarterCommand
{
}
