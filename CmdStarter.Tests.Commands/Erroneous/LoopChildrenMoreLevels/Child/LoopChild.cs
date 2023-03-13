using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopChildrenMoreLevels.ChildChild;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopChildrenMoreLevels.Child
{
    [Parent<LoopChildChild>]
    public class LoopChild : StarterCommand
    {
    }
}
