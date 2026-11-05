using CyberInternauts.CmdStarter.Attributes;

namespace CyberInternauts.CmdStarter.Samples.PlainCommand.Commands;

[Command(Handler = nameof(Show))]
public sealed class Report
{
    public bool Verbose { get; set; }

    public int Show(string topic)
    {
        Console.Out.WriteLine(Verbose ? $"{Describe(topic)}, in detail" : Describe(topic));
        return 0;
    }

    // Another public method: Handler tells which one is the handler.
    public string Describe(string topic) => $"Report on {topic}";
}
