using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Children.DuplicatedChildren.Children;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Children.DuplicatedChildren
{
    [Children<DupChildrenChild>]
    [Children<DupChildrenChild>]
    public class DuplicateOnSame : StarterCommand
    {

    }
}
