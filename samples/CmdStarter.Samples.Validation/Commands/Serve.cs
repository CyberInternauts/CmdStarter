using CyberInternauts.CmdStarter;
using System.ComponentModel.DataAnnotations;

namespace CyberInternauts.CmdStarter.Samples.Validation.Commands;

public sealed class Serve : StarterCommand
{
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
    public int Port { get; set; } = 8080;
    public override Delegate HandlingMethod => (Action)(() => Console.Out.WriteLine($"Serving on port {Port}"));
}

