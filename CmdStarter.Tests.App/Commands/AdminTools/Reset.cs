using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Tests.App.Commands.AdminTools;

public sealed class Reset : StarterCommand
{
    [Option]
    public bool Force { get; set; }

    public override Delegate HandlingMethod => (Action)(() =>
        Console.Out.WriteLine(Force ? "reset forced" : "reset"));
}
