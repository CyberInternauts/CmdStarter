using com.cyberinternauts.csharp.CmdStarter.Lib;
using System.ComponentModel.DataAnnotations;

namespace com.cyberinternauts.csharp.CmdStarter.Demo.Repl.Commands.Account
{
    public sealed class Login : StarterCommand
    {
        public override Delegate HandlingMethod => Execute;

        public int Execute(
            [Required] string username,
            [Required] string password
            )
        {
            if (Authenticator.IsCurrentlyLoggedIn)
            {
                Console.WriteLine("You are already logged in.");
                return -1;
            }

            var loginStatus = Authenticator.Authenticate(username, password);

            switch(loginStatus)
            {
                case Authenticator.LoginStatus.WrongUsername:
                    Console.WriteLine("User doesn't exist.");
                    return -1;
                case Authenticator.LoginStatus.WrongPassword:
                    Console.WriteLine("Incorrect password.");
                    return -1;
                case Authenticator.LoginStatus.LoggedIn:
                    Program.starter.Classes = Program.starter.Classes.Clear();
                    Console.WriteLine($"You are now logged in as {username}.");
                    return 0;
                default:
                    return -1;
            }
        }
    }
}
