using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

// Takes a scoped service, then fails: the scope that created it must still be released.
public sealed class ThrowingConstructorCommand : StarterCommand
{
    public const string FailureMessage = "constructor failure";

    public ThrowingConstructorCommand(ScopedService scoped)
    {
        Scoped = scoped;
        throw new InvalidOperationException(FailureMessage);
    }

    public ScopedService Scoped { get; }
}
