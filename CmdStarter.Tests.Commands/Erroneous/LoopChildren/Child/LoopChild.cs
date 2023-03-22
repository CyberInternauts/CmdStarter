using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopChildren.Parent;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.LoopChildren.Child
{
    [Parent<LoopParent>]
    public class LoopChild : StarterCommand<LoopChild>
    {
    }
}
