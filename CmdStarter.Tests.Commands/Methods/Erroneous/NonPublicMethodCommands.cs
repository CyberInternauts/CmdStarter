namespace CyberInternauts.CmdStarter.Tests.Commands.Methods.Erroneous;

public sealed class NonPublicMethodCommands { [Command] internal void Run() { } }
