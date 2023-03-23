using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopParentMoreLevels.Parent;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopParentMoreLevels.ChildChild
{
    [Parent<LoopParent>]
    public class LoopChildChild : StarterCommand
    {
    }
}
