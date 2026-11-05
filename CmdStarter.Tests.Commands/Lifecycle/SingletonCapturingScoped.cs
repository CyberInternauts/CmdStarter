namespace CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

public sealed class SingletonCapturingScoped(ScopedService scoped)
{
    public ScopedService Scoped { get; } = scoped;
}
