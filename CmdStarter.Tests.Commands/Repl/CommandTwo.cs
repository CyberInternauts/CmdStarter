namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Repl
{
    public sealed class CommandTwo : StarterCommand
    {
        public const int EXPECTED_RETURN = 222;

        public override Delegate HandlingMethod => Execute;

        public int Execute()
        {
            return EXPECTED_RETURN;
        }
    }
}
