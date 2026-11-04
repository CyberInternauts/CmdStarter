using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Methods.Erroneous;

public sealed class MixedModelCommands : StarterCommand { [Command] public void Run() { } }
