using CyberInternauts.CmdStarter.Tests.Commands.Demo.Types;

namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.MultipleParentAttributes;

[Parent<Main>]
[MoreParent<Folders>]
public class MultiParent : StarterCommand
{
}
