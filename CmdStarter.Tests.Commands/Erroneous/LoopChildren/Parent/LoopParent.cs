using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopChildren.Child;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopChildren.Parent
{
    [Parent<LoopChild>]
    public class LoopParent : StarterCommand
    {
    }
}
