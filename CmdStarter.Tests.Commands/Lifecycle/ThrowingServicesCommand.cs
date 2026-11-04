using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

public sealed class ThrowingServicesCommand(ScopedService scoped) : StarterCommand
{
    public ScopedService Scoped { get; } = scoped;
    public override Delegate HandlingMethod => (Action)Execute;
    public void Execute() => throw new InvalidOperationException("Lifecycle test failure");
}
