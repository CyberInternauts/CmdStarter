namespace CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

public sealed class SelfCreatedCommand : IStarterCommand
{
    public SelfCreatedCommand(string origin) => Origin = origin;

    public string Origin { get; }

    public GlobalOptionsManager? GlobalOptionsManager { get; set; }

    public Delegate HandlingMethod => (Action)(() => LifecycleLog.Entries.Add("origin:" + Origin));

    public static IStarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand
        => new SelfCreatedCommand("get-instance");
}
