using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Demo;
using System.ComponentModel.Composition.Hosting;

namespace com.cyberinternauts.csharp.CmdStarter.Tester
{
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            _ = new DirectoryCatalog("."); // This is needed to ensure Tests.Commands assembly is loaded even if no class referenced

            var starter = new CmdStarter.Lib.Starter(
                new string[] {
                    typeof(List).Namespace!, // Only accepts Demo commands
                    typeof(Tests.Commands.GlobalOptions.MainGlobalOptions).Namespace!,
                }
            );
            var exitCode = await starter.Start(args);

            Console.WriteLine("Hello, World!");
            return exitCode;
        }
    }
}
