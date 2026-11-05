namespace CyberInternauts.CmdStarter.Tests.Commands.Loader.ChildingByInterface;

public class ChildingParentByInterface : IStarterCommand
{
    public GlobalOptionsManager? GlobalOptionsManager { get; set; }

    public Delegate HandlingMethod => IStarterCommand.EmptyExecution;
}
