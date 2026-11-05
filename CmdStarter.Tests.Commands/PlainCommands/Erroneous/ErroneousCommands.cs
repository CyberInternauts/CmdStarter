namespace CyberInternauts.CmdStarter.Tests.Commands.PlainCommands.Erroneous;

[Command]
public sealed class ZeroMethodsPlain
{
}

[Command]
public sealed class TwoMethodsPlain
{
    public int First() => 1;

    public int Second() => 2;
}

[Command]
public sealed class BothModelsPlain : StarterCommand
{
    public override Delegate HandlingMethod => () => 0;
}

[Command("two words")]
public sealed class SpaceNamePlain
{
    public int Run() => 0;
}

[Command("   ")]
public sealed class BlankNamePlain
{
    public int Run() => 0;
}

[Command]
public sealed class GenericMethodPlain
{
    public int Run<T>() => 0;
}

[Command]
public sealed class RefParameterPlain
{
    public int Run(ref int value) => value;
}
