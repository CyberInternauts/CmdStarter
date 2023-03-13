namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.TreeWithChildrenAttribute
{
    [Parent(null)]
    [Children<ChildrenOfParent.ChildNoParent>]
    public class ParentWithChildren : StarterCommand
    {
    }
}
