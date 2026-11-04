using CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopChildrenMoreLevels.ChildChild;

namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopChildrenMoreLevels.Child;

[Parent<LoopChildChild>]
public class LoopChild : StarterCommand
{
}
