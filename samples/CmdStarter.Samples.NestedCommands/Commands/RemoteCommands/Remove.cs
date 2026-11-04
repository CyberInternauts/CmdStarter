using com.cyberinternauts.csharp.CmdStarter.Lib;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.NestedCommands.Commands.RemoteCommands;

public sealed class Remove : StarterCommand { public override Delegate HandlingMethod => (Action<string>)(name => Console.Out.WriteLine($"Removed remote {name}")); }
