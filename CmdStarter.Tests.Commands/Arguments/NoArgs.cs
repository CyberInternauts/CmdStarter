namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Arguments
{
    public class NoArgs : StarterCommand
    {
        public override Delegate HandlingMethod => () => { };
    }
}
