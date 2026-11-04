using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Samples.TwoCommands.Commands;

public sealed class Subtract : StarterCommand
{
    public override Delegate HandlingMethod => (Action<int, int>)((left, right) => Console.Out.WriteLine(left - right));
}

