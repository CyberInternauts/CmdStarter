using com.cyberinternauts.csharp.CmdStarter.Demo.Repl.Commands.PreLogin;
using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Demo;
using System.ComponentModel.Composition.Hosting;

namespace com.cyberinternauts.csharp.CmdStarter.Demo.Repl
{
    internal class Program
    {
        public static Starter starter = new CmdStarter.Lib.Starter(
                new string[] { typeof(Login).Namespace! }
            );

        public static async Task Main(string[] args)
        {
            _ = new DirectoryCatalog("."); // This is needed to ensure Tests.Commands assembly is loaded even if no class referenced

            starter.IsRootingLonelyCommand = false;

            var a = await starter.Start("-h");

            Console.WriteLine(a);
        }
    }
}