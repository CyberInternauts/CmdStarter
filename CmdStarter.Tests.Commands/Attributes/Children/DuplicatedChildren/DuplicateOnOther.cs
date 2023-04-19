using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Children.DuplicatedChildren.Children;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Children.DuplicatedChildren
{
    [Children<DupChildrenChild>]
    public class DuplicateOnOther : StarterCommand
    {
    }
}
