using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Tests.Commands.Loader.ByInterface;

public class FirstByInterface : IStarterCommand
{
    public GlobalOptionsManager? GlobalOptionsManager { get; set; }

    public Delegate HandlingMethod => IStarterCommand.EmptyExecution;

}
