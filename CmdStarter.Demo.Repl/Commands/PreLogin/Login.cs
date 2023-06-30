using com.cyberinternauts.csharp.CmdStarter.Demo.Repl.Commands.PostLogin;
using com.cyberinternauts.csharp.CmdStarter.Lib;
using System.ComponentModel.DataAnnotations;

namespace com.cyberinternauts.csharp.CmdStarter.Demo.Repl.Commands.PreLogin
{
    public sealed class Login : StarterCommand
    {
        private static Dictionary<string, string> accounts = new()
        {
            { "admin", "admin" },
        };

        public override Delegate HandlingMethod => Execute;

        public int Execute(
            [Required] string username,
            [Required] string password
            )
        {
            if(accounts.ContainsKey( username ) )
            {
                if (accounts[ username ] != password)
                {
                    Console.WriteLine("Incorrect password.");
                    return 0;
                }

                Program.starter.Namespaces = Program.starter.Namespaces.Clear()
                    .Add(typeof(Add).Namespace!);

                return 0;
            }

            Console.WriteLine("Account doesn't exist.");
            return -1;
        }
    }
}
