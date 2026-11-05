namespace CyberInternauts.CmdStarter.Tests.Commands.Options;

public class OptByInterface : IStarterCommand
{
    public int IntOpt { get; set; } = 111;
    public const string INT_OPT_KEBAB = "int-opt";

    public GlobalOptionsManager? GlobalOptionsManager { get; set; }

    public Delegate HandlingMethod => IStarterCommand.EmptyExecution;
}
