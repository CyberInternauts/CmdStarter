using com.cyberinternauts.csharp.CmdStarter.Lib;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace com.cyberinternauts.csharp.CmdStarter.Demo.Repl.Commands.PostLogin
{
    public sealed class Add : StarterCommand
    {
        public override Delegate HandlingMethod => base.HandlingMethod;

        public int Execute(
            [Required][Description("The name of the book to be added.")]
            string name)
        {
            if (DemoDb.Books.Add(name))
            {
                Console.WriteLine($"Book: {name} added.");
                return 0;
            }

            Console.WriteLine($"Book: {name} already exists.");
            return -1;
        }
    }
}
