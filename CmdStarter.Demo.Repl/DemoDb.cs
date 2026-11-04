namespace CyberInternauts.CmdStarter.Demo.Repl;

public static class DemoDb
{
    public static HashSet<string> Books { get; } = [];

    public static Dictionary<string, string> Accounts = new()
    {
        { "admin", "admin" },
    };
}
