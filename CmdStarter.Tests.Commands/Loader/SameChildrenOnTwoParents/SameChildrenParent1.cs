using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.SameChildrenOnTwoParents.Children;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.SameChildrenOnTwoParents
{
    [Children<SameChildrenChild>]
    public class SameChildrenParent1 : StarterCommand
    {
    }
}
