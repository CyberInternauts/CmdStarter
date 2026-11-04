using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Samples.Validation.Commands;

public sealed class Serve : StarterCommand
{
    public int Port { get; set; } = 8080;
    public override Delegate HandlingMethod => (Action)(() => Console.Out.WriteLine($"Serving on port {Port}"));
}

