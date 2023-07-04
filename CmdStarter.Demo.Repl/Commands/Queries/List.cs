using com.cyberinternauts.csharp.CmdStarter.Lib;

namespace com.cyberinternauts.csharp.CmdStarter.Demo.Repl.Commands.Queries
{
    public sealed class List : StarterCommand
    {
        public override string? Description => "Lists all the books in the database.";

        public override Delegate HandlingMethod => Execute;

        public int Execute()
        {
            if (!Authenticator.IsCurrentlyLoggedIn)
            {
                Console.WriteLine("You are not logged in.");
                return -1;
            }

            foreach (var book in DemoDb.Books)
            {
                Console.WriteLine(book);
            }

            return 0;
        }
    }
}
