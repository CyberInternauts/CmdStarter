using CyberInternauts.CmdStarter;

namespace CyberInternauts.CmdStarter.Demo.Repl.Commands.Account;

[CyberInternauts.CmdStarter.Attributes.AvailableInState(DemoState.LoggedIn)]
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
