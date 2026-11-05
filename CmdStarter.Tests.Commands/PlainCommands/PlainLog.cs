namespace CyberInternauts.CmdStarter.Tests.Commands.PlainCommands;

public static class PlainLog
{
    public static List<string> Entries { get; } = [];

    public static List<object> Received { get; } = [];

    public static void Reset()
    {
        Entries.Clear();
        Received.Clear();
    }
}
