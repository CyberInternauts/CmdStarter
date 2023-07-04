namespace com.cyberinternauts.csharp.CmdStarter.Demo.Repl
{
    public static class DemoDb
    {
        public static HashSet<string> Books { get; } = new();

        public static Dictionary<string, string> Accounts = new()
        {
            { "admin", "admin" },
        };
    }
}
