using CmdStarter.Demo.SimpleInjector.Commands;
using CmdStarter.Demo.SimpleInjector.Services;
using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using SimpleInjector;

namespace CmdStarter.Demo.SimpleInjector
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var container = new Container();

            // Register services
            container.Register<IService, Service2>(Lifestyle.Singleton);

            // Set factory
            var creationMethod = (Type commandType) =>
            {
                return container.GetInstance(commandType) as IStarterCommand;
            };
            Starter.SetFactory(creationMethod);

            // Find commands
            var starter = new Starter();
            starter.FindCommandsTypes();

            // Place the code here because I should iterate through all commands
            container.Register<CmdService>(Lifestyle.Singleton);

            await starter.Start(args);
        }
    }
}