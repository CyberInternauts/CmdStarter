using CyberInternauts.CmdStarter.Attributes;
using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Samples.NestedCommands.Settings;

[Parent<Config>]
public sealed class Get : StarterCommand
{
    public override Delegate HandlingMethod => (Action<string>)(key => Console.Out.WriteLine($"{key}=default"));
}

