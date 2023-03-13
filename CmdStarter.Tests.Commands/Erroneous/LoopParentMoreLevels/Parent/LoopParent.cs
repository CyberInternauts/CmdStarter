using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopParentMoreLevels.Child;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopParentMoreLevels.Parent
{
    [Parent<LoopChild>]
    public class LoopParent : StarterCommand
    {
    }
}
