namespace CyberInternauts.CmdStarter.Tests.Commands.Erroneous.NameConflicts.MethodAlias;

public sealed class Pong : StarterCommand
{
    public override Delegate HandlingMethod => (Func<int>)(() => 0);
}

public static class PingCommands
{
    [Command]
    [Alias("pong")]
    public static int Ping() => 0;
}
