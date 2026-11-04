using CyberInternauts.CmdStarter.Attributes;
namespace CyberInternauts.CmdStarter.Tests.Commands.Methods.Erroneous;

public sealed class RefParameterCommands { [Command] public void Run(ref int value) { } }
