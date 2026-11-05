using CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopParentMoreLevels.ChildChild;

namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopParentMoreLevels.Child;

[Parent<LoopChildChild>]
public class LoopChild : StarterCommand
{
}
