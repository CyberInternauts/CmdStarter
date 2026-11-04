using com.cyberinternauts.csharp.CmdStarter.Lib;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Lifecycle;

public sealed class ThrowingServicesCommand(ScopedService scoped) : StarterCommand
{
    public ScopedService Scoped { get; } = scoped;
    public override Delegate HandlingMethod => (Action)Execute;
    public void Execute() => throw new InvalidOperationException("Lifecycle test failure");
}
