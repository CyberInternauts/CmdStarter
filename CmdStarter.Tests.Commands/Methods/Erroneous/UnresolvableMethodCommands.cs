namespace CyberInternauts.CmdStarter.Tests.Commands.Methods.Erroneous;

public sealed class UnresolvableMethodCommands(IComparable notRegistered)
{
    [Command]
    public void Run() { _ = notRegistered; }
}
