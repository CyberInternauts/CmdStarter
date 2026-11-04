namespace CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

public sealed class ScopedConsumer(ScopedService scoped)
{
    public ScopedService Scoped { get; } = scoped;
}
