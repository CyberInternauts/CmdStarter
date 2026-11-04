using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Lib;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.NestedCommands.Settings;

[Parent<Config>]
public sealed class Get : StarterCommand { public override Delegate HandlingMethod => (Action<string>)(key => Console.Out.WriteLine($"{key}=default")); }

