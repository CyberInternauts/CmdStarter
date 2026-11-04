namespace CyberInternauts.CmdStarter.Tests.Commands.Methods;

public static class MethodsLog
{
    public static List<string> Entries { get; } = [];
    public static void Reset() => Entries.Clear();
}
