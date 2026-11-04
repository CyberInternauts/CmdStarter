namespace CyberInternauts.CmdStarter.Tests.Commands.Loader.ChildingByInterface.Children;

[Parent<ChildingParentByInterface>]
public class ChildingChildByInterface : IStarterCommand
{
    public GlobalOptionsManager? GlobalOptionsManager { get; set; }

    public Delegate HandlingMethod => IStarterCommand.EmptyExecution;
}
