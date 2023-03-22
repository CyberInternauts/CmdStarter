namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments
{
    public class NoArgs : StarterCommand<NoArgs>
    {
        public override Delegate MethodForHandling => () => { };
    }
}
