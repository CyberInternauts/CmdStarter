using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments.Child;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments
{
    [Children<ArgChild>]
    public class ArgParent : StarterCommand<ArgParent>
    {
        public override Delegate MethodForHandling => (string param1) => { };
    }
}
