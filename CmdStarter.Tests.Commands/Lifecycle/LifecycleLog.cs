namespace CyberInternauts.CmdStarter.Tests.Commands.Lifecycle;

public static class LifecycleLog
{
    private static int counter;

    public static List<string> Entries { get; } = [];

    public static int NextId() => ++counter;

    public static void Reset()
    {
        Entries.Clear();
        counter = 0;
    }
}
