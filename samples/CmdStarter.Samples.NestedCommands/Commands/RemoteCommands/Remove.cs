namespace CyberInternauts.CmdStarter.Samples.NestedCommands.Commands.RemoteCommands;

public sealed class Remove : StarterCommand
{
    public override Delegate HandlingMethod => (Action<string>)(name =>
        Console.Out.WriteLine($"Removed remote {name}"));
}

