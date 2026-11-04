using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.GlobalOptions;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Methods;

public static class ToolCommands
{
    [Command("ping")] public static int Ping() => 7;
    [Command, Alias("st")] public static int Status(MainGlobalOptions globals) => globals.IntGlobalOption;
    [Command] public static async ValueTask<int> WaitAsync() { await Task.Yield(); return 3; }
}
