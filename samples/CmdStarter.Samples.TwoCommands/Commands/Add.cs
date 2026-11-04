using com.cyberinternauts.csharp.CmdStarter.Lib;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.TwoCommands.Commands;

public sealed class Add : StarterCommand { public override Delegate HandlingMethod => (Action<int, int>)((left, right) => Console.Out.WriteLine(left + right)); }
