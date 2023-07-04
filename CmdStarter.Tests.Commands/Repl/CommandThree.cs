namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Repl
{
    public sealed class CommandThree : StarterCommand
    {
        public const int EXPECTED_RETURN = 333;

        public override Delegate HandlingMethod => Execute;

        public int Execute()
        {
            return EXPECTED_RETURN;
        }
    }
}
