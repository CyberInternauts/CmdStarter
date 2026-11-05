namespace CyberInternauts.CmdStarter.Tests.Commands.PlainCommands;

// Handler names the handler, so the class may have other public methods.
[Command(Handler = nameof(Run))]
public sealed class DesignatedPlain
{
    public int Helper() => 1;

    public int Run() => 81;

    public int Another(int value) => value;
}

public abstract class HandlerBase
{
    public int Inherited() => 82;
}

[Command(Handler = nameof(Inherited))]
public sealed class InheritedHandlerPlain : HandlerBase
{
    public int Other() => 0;
}

[Command("named-designated", Handler = nameof(Go))]
public sealed class NamedDesignatedPlain
{
    public int Go() => 83;

    public int Stop() => 0;
}
