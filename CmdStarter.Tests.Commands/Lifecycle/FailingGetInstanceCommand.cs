namespace CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

public sealed class FailingGetInstanceCommand : StarterCommand
{
    public static new IStarterCommand GetInstance<TCommand>() where TCommand : IStarterCommand => throw new InvalidOperationException("GetInstance failed");

    public override Delegate HandlingMethod => () => 0;
}