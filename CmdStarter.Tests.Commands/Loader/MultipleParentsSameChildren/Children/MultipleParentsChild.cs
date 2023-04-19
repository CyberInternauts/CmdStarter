namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.MultipleParentsSameChildren.Children
{
    [Parent<MultipleParent1>]
    [Parent<MultipleParent2>]
    public class MultipleParentsChild : StarterCommand
    {
    }
}
