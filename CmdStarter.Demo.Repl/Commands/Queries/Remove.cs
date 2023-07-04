using com.cyberinternauts.csharp.CmdStarter.Lib;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace com.cyberinternauts.csharp.CmdStarter.Demo.Repl.Commands.Queries
{
    public sealed class Remove : StarterCommand
    {
        public override string? Description => "Removes a book from the database.\nCan only be used when logged in.";

        public override Delegate HandlingMethod => Execute;

        public int Execute(
            [Required][Description("The name of the book to be removed.")]
            string name)
        {
            if (!Authenticator.IsCurrentlyLoggedIn)
            {
                Console.WriteLine("You are not logged in.");
                return -1;
            }

            if (DemoDb.Books.Remove(name))
            {
                Console.WriteLine($"Book: {name} removed");
                return 0;
            }

            Console.WriteLine($"Book: {name} doesn't exist.");
            return -1;
        }
    }
}
