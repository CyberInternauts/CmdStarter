using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Lib;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.NestedCommands.Settings;

[Parent<Config>]
public sealed class Set : StarterCommand { public override Delegate HandlingMethod => (Action<string, string>)((key, value) => Console.Out.WriteLine($"{key}={value}")); }

