using com.cyberinternauts.csharp.CmdStarter.Lib;

namespace com.cyberinternauts.csharp.CmdStarter.Demo.Repl.Commands.Account;

[com.cyberinternauts.csharp.CmdStarter.Lib.Attributes.AvailableInState(DemoState.LoggedIn)]
public sealed class Logout : StarterCommand
{
    public override Delegate HandlingMethod => Execute;

    public int Execute()
    {
        Authenticator.LogOut();
        Console.WriteLine("You are now logged out.");
        return 0;
    }
}
