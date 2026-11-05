using CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopChildren.Child;

namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.LoopChildren.Parent;

[Parent<LoopChild>]
public class LoopParent : StarterCommand
{
}
