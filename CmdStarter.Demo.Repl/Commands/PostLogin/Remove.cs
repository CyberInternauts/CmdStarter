using com.cyberinternauts.csharp.CmdStarter.Lib;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace com.cyberinternauts.csharp.CmdStarter.Demo.Repl.Commands.PostLogin
{
    public sealed class Remove : StarterCommand
    {
        public override Delegate HandlingMethod => base.HandlingMethod;

        public int Execute(
            [Required][Description("The name of the book to be removed.")]
            string name)
        {
            if(DemoDb.Books.Remove(name))
            {
                Console.WriteLine($"Book: {name} removed");
                return 0;
            }

            Console.WriteLine($"Book: {name} doesn't exist.");
            return -1;
        }
    }
}
