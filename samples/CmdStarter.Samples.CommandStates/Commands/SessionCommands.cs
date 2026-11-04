using com.cyberinternauts.csharp.CmdStarter.Lib.Attributes;
using com.cyberinternauts.csharp.CmdStarter.Samples.CommandStates.Services;

namespace com.cyberinternauts.csharp.CmdStarter.Samples.CommandStates.Commands;

public sealed class SessionCommands
{
    [Command]
    [AvailableInState(SessionState.LoggedOut)]
    public static void Login(string user, Session session)
    {
        session.User = user;
        session.State = SessionState.LoggedIn;
        Console.WriteLine($"Welcome, {user}.");
    }

    [Command]
    [AvailableInState(SessionState.LoggedIn)]
    public static void Whoami(Session session)
    {
        Console.WriteLine($"You are {session.User}.");
    }

    [Command]
    [AvailableInState(SessionState.LoggedIn)]
    public static void Logout(Session session)
    {
        Console.WriteLine($"Goodbye, {session.User}.");
        session.User = null;
        session.State = SessionState.LoggedOut;
    }
}
