namespace CyberInternauts.CmdStarter.Tests.Commands.PlainCommands;

[Command]
public sealed class TopPlain
{
    public int Run() => 41;
}

[Parent<TopPlain>]
[Command]
public sealed class ChildPlain
{
    public int Run() => 42;
}

public sealed class ClassTop : StarterCommand
{
    public override Delegate HandlingMethod => () => 43;
}

[Parent<ClassTop>]
[Command]
public sealed class PlainUnderClass
{
    public int Run() => 44;
}

[Parent(typeof(TopPlain))]
public sealed class ClassUnderPlain : StarterCommand
{
    public override Delegate HandlingMethod => () => 45;
}
