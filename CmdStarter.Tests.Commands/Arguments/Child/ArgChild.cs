namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments.Child
{
    [Parent<ArgParent>]
    public class ArgChild : StarterCommand<ArgChild>
    {
        public override Delegate MethodForHandling => (int p1) => { };
    }
}
