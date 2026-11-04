using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Lib;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.App.Commands.AdminTools;

public sealed class Reset : StarterCommand
{
    [Option]
    public bool Force { get; set; }

    public override Delegate HandlingMethod => (Action)(() =>
        Console.Out.WriteLine(Force ? "reset forced" : "reset"));
}
