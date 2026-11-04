using CyberInternauts.CmdStarter.Tests.Commands.TreeBuilding.ChildrenLoop.Second;

namespace CyberInternauts.CmdStarter.Tests.Commands.TreeBuilding.ChildrenLoop.First;

// The children of this command are the commands under the namespace of LoopSecond, whose children are the commands under this namespace.
[Children<LoopSecond>]
public class LoopFirst : StarterCommand
{
}
