namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Methods;

public static class MethodsLog
{
    public static List<string> Entries { get; } = new();
    public static void Reset() => Entries.Clear();
}
