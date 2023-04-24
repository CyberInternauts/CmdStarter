using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments.Child;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments
{
    [Children<ArgChild>]
    public class ArgParent : StarterCommand
    {
        public override Delegate HandlingMethod => (string param1) => { };
    }
}
