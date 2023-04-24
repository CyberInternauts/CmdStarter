namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments.Child
{
    [Parent<ArgParent>]
    public class ArgChild : StarterCommand
    {
        public override Delegate HandlingMethod => (int p1) => { };
    }
}
