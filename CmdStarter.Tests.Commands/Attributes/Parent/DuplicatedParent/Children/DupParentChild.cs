namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Parent.DuplicatedParent.Children
{
    [Parent<DupParent>]
    [Parent<DupParent>]
    public class DupParentChild : StarterCommand
    {
    }
}
