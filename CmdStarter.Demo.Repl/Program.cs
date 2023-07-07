using com.cyberinternauts.csharp.CmdStarter.Demo.Repl.Commands.Account;
using com.cyberinternauts.csharp.CmdStarter.Lib.Repl;
using System.ComponentModel.Composition.Hosting;
using System.Threading.Channels;

namespace com.cyberinternauts.csharp.CmdStarter.Demo.Repl
{
    internal class Program
    {
        public static ReplStarter starter = new ReplStarter(
                new ConsoleReplProvider(),
                new string[] { typeof(Program).Namespace! }
            );

        public static async Task Main(string[] args)
        {
            _ = new DirectoryCatalog("."); // This is needed to ensure Tests.Commands assembly is loaded even if no class referenced

            starter.IsRootingLonelyCommand = false;

            starter.Classes = starter.Classes.Clear().Add(typeof(Login).FullName!);

            starter.OnCommandExecuted += (sender, eventArgs) => Console.WriteLine();

            args = new[] { "-h" };

            await starter.Launch(args);
        }
    }
}