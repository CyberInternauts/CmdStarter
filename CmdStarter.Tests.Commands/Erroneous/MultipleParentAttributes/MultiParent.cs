using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Demo.Types;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Erroneous.MultipleParentAttributes
{
    [Parent<Main>]
    [MoreParent<Folders>]
    public class MultiParent : StarterCommand
    {
    }
}
