using com.cyberinternauts.csharp.CmdStarter.Lib;

namespace com.cyberinternauts.csharp.CmdStarter.Demo.Repl.Commands.Account
{
    public sealed class Logout : StarterCommand
    {
        public override Delegate HandlingMethod => Execute;

        public int Execute()
        {
            if (!Authenticator.IsCurrentlyLoggedIn)
            {
                Console.WriteLine("You are not logged in.");
                return -1;
            }

            Authenticator.LogOut();
            Console.WriteLine("You are now logged out.");
            return 0;
        }
    }
}
