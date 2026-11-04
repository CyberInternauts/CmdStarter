using com.cyberinternauts.csharp.CmdStarter.Lib;

namespace com.cyberinternauts.csharp.CmdStarter.Samples.NestedCommands.Commands.RemoteCommands;

public sealed class Add : StarterCommand
{
    public override Delegate HandlingMethod => (Action<string, string>)((name, url) =>
        Console.Out.WriteLine($"Added remote {name} ({url})"));
}

