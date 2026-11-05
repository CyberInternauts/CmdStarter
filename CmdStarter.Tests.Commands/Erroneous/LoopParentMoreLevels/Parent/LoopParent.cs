using CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopParentMoreLevels.Child;

namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopParentMoreLevels.Parent;

[Parent<LoopChild>]
public class LoopParent : StarterCommand
{
}
