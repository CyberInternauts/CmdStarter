using CyberInternauts.CmdStarter.Repl;
using CyberInternauts.CmdStarter.Demo.Repl.Commands;
using System.ComponentModel.Composition.Hosting;

namespace CyberInternauts.CmdStarter.Demo.Repl;

internal class Program
{
    public static ReplStarter starter = new ReplStarter(
            new ConsoleReplProvider(),
            new string[] { typeof(Program).Namespace! }
        );

    public static async Task Main(string[] args)
    {
        _ = new DirectoryCatalog("."); // This is needed to ensure Tests.Commands assembly is loaded even if no class referenced

        starter.StateProvider = new AuthenticatorStateProvider();


        starter.OnCommandExecuted += (sender, eventArgs) => Console.WriteLine();

        args = ["-h"];

        await starter.Launch(args);
    }
}
