using com.cyberinternauts.csharp.CmdStarter.Tests.Common.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Repl
{
    public sealed class PreCommand : StarterCommand, IHasExpectedValue<int>
    {
        public const int EXPECTED_RETURN = -111;

        public override Delegate HandlingMethod => Execute;

        public int ExpectedValue => EXPECTED_RETURN;

        public int Execute()
        {
            return EXPECTED_RETURN;
        }
    }
}
