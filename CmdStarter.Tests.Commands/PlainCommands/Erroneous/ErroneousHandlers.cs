namespace CyberInternauts.CmdStarter.Tests.Commands.PlainCommands.Erroneous;

[Command(Handler = nameof(Run))]
public sealed class OverloadedHandlerPlain
{
    public int Run() => 0;

    public int Run(int value) => value;
}

[Command(Handler = "Missing")]
public sealed class MissingHandlerPlain
{
    public int Run() => 0;
}

[Command(Handler = nameof(Run))]
public sealed class PrivateHandlerPlain
{
    private int Run() => 0;

    public int Other() => 0;
}

[Command(Handler = nameof(Run))]
public sealed class StaticHandlerPlain
{
    public static int Run() => 0;

    public int Other() => 0;
}

[Command(Handler = nameof(Run))]
public sealed class GenericHandlerPlain
{
    public int Run<T>() => 0;

    public int Other() => 0;
}

[Command(Handler = nameof(Run))]
public sealed class RefHandlerPlain
{
    public int Run(ref int value) => value;

    public int Other() => 0;
}

[Command("handled-group", Handler = nameof(List))]
public sealed class HandlerOnGroup
{
    [Command]
    public static int List() => 0;
}

[Command("method-holder")]
public sealed class HandlerOnMethod
{
    [Command(Handler = "Whatever")]
    public static int Do() => 0;
}
