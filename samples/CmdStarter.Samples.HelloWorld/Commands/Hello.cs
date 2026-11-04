using com.cyberinternauts.csharp.CmdStarter.Lib;
namespace com.cyberinternauts.csharp.CmdStarter.Samples.HelloWorld.Commands;

public sealed class Hello : StarterCommand
{
    public string To { get; set; } = "World";
    public override Delegate HandlingMethod => (Action)(() => Console.Out.WriteLine($"Hello, {To}!"));
}
