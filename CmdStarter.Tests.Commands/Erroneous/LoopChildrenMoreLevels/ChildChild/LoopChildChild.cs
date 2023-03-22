using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopChildrenMoreLevels.Parent;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopChildrenMoreLevels.ChildChild
{
    [Parent<LoopParent>]
    public class LoopChildChild : StarterCommand<LoopChildChild>
    {
    }
}
