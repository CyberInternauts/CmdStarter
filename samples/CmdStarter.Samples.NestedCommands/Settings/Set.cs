using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Samples.NestedCommands.Settings;

[Parent<Config>]
public sealed class Set : StarterCommand
{
    public override Delegate HandlingMethod => (Action<string, string>)((key, value) =>
        Console.Out.WriteLine($"{key}={value}"));
}

