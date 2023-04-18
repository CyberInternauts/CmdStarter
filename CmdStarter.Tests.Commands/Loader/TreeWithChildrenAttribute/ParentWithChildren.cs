using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.TreeWithChildrenAttribute.ChildrenOfParent;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.TreeWithChildrenAttribute
{
    [Parent(null)]
    [Children<ChildNoParent>]
    public class ParentWithChildren : StarterCommand
    {
    }
}
