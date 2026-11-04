using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Samples.NestedCommands.Commands.RemoteCommands;

public sealed class Add : StarterCommand
{
    public override Delegate HandlingMethod => (Action<string, string>)((name, url) =>
        Console.Out.WriteLine($"Added remote {name} ({url})"));
}

