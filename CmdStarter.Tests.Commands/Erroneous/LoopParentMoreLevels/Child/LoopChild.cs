using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopParentMoreLevels.ChildChild;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopParentMoreLevels.Child
{
    [Parent<LoopChildChild>]
    public class LoopChild : StarterCommand
    {
    }
}
