using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Samples.Repl.Services;

namespace com.cyberinternauts.csharp.CmdStarter.Samples.Repl.Commands;

public sealed class SessionCommands
{
    [Command]
    public static void Login(string user, Session session, CommandScope scope)
    {
        session.User = user;
        Console.WriteLine($"Logged in as {user} (scope {scope.Number})");
    }

    [Command]
    public static void Whoami(Session session, CommandScope scope)
    {
        Console.WriteLine($"{session.User ?? "anonymous"} (scope {scope.Number})");
    }
}
