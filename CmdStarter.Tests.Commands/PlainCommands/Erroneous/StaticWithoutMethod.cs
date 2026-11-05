namespace CyberInternauts.CmdStarter.Tests.Commands.PlainCommands.Erroneous;

// A static class cannot be a plain command, so it is a group of the method model, and it has no method.
[Command]
public static class StaticWithoutMethod
{
}
