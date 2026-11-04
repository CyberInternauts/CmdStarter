using CyberInternauts.CmdStarter.Tests.Commands.Loader.TreeWithChildrenAttribute.ChildrenOfParent;

namespace CyberInternauts.CmdStarter.Tests.Commands.Loader.TreeWithChildrenAttribute;

[Parent(null)]
[Children<ChildNoParent>]
public class ParentWithChildren : StarterCommand
{
}
