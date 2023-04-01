using System.ComponentModel.Composition.Hosting;

namespace com.cyberinternauts.csharp.CmdStarter.Tester
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            _ = new DirectoryCatalog("."); // This is needed to ensure Tests.Commands assembly is loaded even if no class referenced

            var starter = new CmdStarter.Lib.Starter(
                new string[] {
                    typeof(Tests.Commands.Listing.List).Namespace!, // Only accepts Listing commands
                    typeof(Tests.Commands.GlobalOptions.MainGlobalOptions).Namespace!,
                }
            );
            await starter.Start(args);

            Console.WriteLine("Hello, World!");
        }
    }
}