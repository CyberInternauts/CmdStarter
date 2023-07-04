namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Repl
{
    public sealed class PreCommand : StarterCommand
    {
        public const int EXPECTED_RETURN = -111;

        public override Delegate HandlingMethod => Execute;

        public int Execute()
        {
            return EXPECTED_RETURN;
        }
    }
}
