using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Children.DuplicatedChildren.Children;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Attributes.Children.DuplicatedChildren
{
    [Children<DupChild>]
    [Children<DupChild>]
    public class DuplicateOnSame : StarterCommand
    {

    }
}
