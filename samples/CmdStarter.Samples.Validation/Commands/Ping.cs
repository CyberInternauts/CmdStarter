using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Samples.Validation.Commands;

public sealed class Ping : StarterCommand
{
    public int Count { get; set; } = 4;
    public override Delegate HandlingMethod
    {
        get => (Action<string>)(host => Console.Out.WriteLine($"Pinging {host} {Count} times"));
    }
}

