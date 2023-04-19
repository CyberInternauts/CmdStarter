using com.cyberinternauts.csharp.CmdStarter.Lib.Exceptions;
using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.SelfParent
{
    [Parent<SelfParentClass>]
    public class SelfParentClass : StarterCommand
    {
    }
}
