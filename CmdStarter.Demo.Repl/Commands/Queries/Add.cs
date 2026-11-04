using CyberInternauts.CmdStarter;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CyberInternauts.CmdStarter.Demo.Repl.Commands.Queries;

[Description("Adds a book from the database.\nCan only be used when logged in.")]
[CyberInternauts.CmdStarter.Attributes.AvailableInState(DemoState.LoggedIn)]
public sealed class Add : StarterCommand
{

    public override Delegate HandlingMethod => Execute;

    public int Execute(
        [Required][Description("The names of the book to be added.")]
        string[] books)
    {
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
