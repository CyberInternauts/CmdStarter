using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Samples.PlainCommand.Commands;

[Command]
public sealed class Greet
{
    public string Greeting { get; set; } = "Hello";

    public int Run(string name)
    {
        Console.Out.WriteLine($"{Greeting}, {name}!");
        return 0;
    }
}
