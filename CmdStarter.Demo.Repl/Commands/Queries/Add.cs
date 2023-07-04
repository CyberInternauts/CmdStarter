using com.cyberinternauts.csharp.CmdStarter.Lib;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace com.cyberinternauts.csharp.CmdStarter.Demo.Repl.Commands.Queries
{
    public sealed class Add : StarterCommand
    {
        public override string? Description => "Adds a book from the database.\nCan only be used when logged in.";

        public override Delegate HandlingMethod => Execute;

        public int Execute(
            [Required][Description("The names of the book to be added.")]
            string[] books)
        {
            if(!Authenticator.IsCurrentlyLoggedIn)
            {
                Console.WriteLine("You are not logged in.");
                return -1;
            }

            foreach (var book in books)
            {
                if (DemoDb.Books.Add(book))
                {
                    Console.WriteLine($"Book: {book} added.");
                    continue;
                }

                Console.WriteLine($"Book: {book} already exists. Continue (c) or stop (s)?");

                var input = Console.ReadKey();

                if (input.KeyChar == 'c') continue;

                Console.WriteLine("Aborted.");
                return -1;
            }

            return 0;
        }
    }
}
