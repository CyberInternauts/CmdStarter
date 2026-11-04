using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Attributes;
namespace CyberInternauts.CmdStarter.Tests.Commands.Methods.Erroneous;

public sealed class MixedModelCommands : StarterCommand { [Command] public void Run() { } }
