using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopChildrenMoreLevels.Child;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopChildrenMoreLevels.Parent
{
    [Parent<LoopChild>]
    public class LoopParent : StarterCommand
    {
    }
}
