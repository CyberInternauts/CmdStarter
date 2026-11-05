using CyberInternauts.CmdStarter.Tests.Commands.Attributes.Children.DuplicatedChildren.Children;

namespace CyberInternauts.CmdStarter.Tests.Commands.Attributes.Children.DuplicatedChildren;

[Children<DupChild>]
[Children<DupChild>]
public class DuplicateOnSame : StarterCommand
{

}
