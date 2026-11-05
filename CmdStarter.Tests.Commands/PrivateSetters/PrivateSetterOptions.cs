namespace CyberInternauts.CmdStarter.Tests.Commands.PrivateSetters;

public sealed class PrivateSetterOptions : StarterCommand
{
    public int WithPrivateSetter { get; private set; }

    public int WithInit { get; init; }

    public int ReadOnly { get; }

    public override Delegate HandlingMethod => (Func<int>)(() => WithPrivateSetter * 10 + WithInit);
}
