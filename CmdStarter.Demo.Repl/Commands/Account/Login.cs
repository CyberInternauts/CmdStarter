using CyberInternauts.CmdStarter;
using System.ComponentModel.DataAnnotations;

namespace CyberInternauts.CmdStarter.Demo.Repl.Commands.Account;

[CyberInternauts.CmdStarter.Attributes.AvailableInState(DemoState.LoggedOut)]
public sealed class Login : StarterCommand
{
    public override Delegate HandlingMethod => Execute;

    public int Execute(
        [Required] string username,
        [Required] string password
        )
    {
        var loginStatus = Authenticator.Authenticate(username, password);

        switch (loginStatus)
        {
            case Authenticator.LoginStatus.WrongUsername:
                Console.WriteLine("User doesn't exist.");
                return -1;
            case Authenticator.LoginStatus.WrongPassword:
                Console.WriteLine("Incorrect password.");
                return -1;
            case Authenticator.LoginStatus.LoggedIn:
                Console.WriteLine($"You are now logged in as {username}.");
                return 0;
            default:
                return -1;
        }
    }
}
