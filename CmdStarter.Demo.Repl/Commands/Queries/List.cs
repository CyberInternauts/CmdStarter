using System.ComponentModel;

namespace CyberInternauts.CmdStarter.Demo.Repl.Commands.Queries;

[Description("Lists all the books in the database.")]
[CyberInternauts.CmdStarter.Attributes.AvailableInState(DemoState.LoggedIn)]
public sealed class List : StarterCommand
{

    public override Delegate HandlingMethod => Execute;

    public int Execute()
    {
        foreach (var book in DemoDb.Books)
        {
            Console.WriteLine(book);
        }

        return 0;
    }
}
