using CmdStarter.Demo.SimpleInjector.Commands;
using CmdStarter.Demo.SimpleInjector.Services;
using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;
using SimpleInjector;

namespace CmdStarter.Demo.SimpleInjector
{
    internal class Program : IServiceManager
    {
        private Starter starter = new();
        private Container container = new();

        internal Program()
        {
            // Register services
            container.Register<IService, Service2>(Lifestyle.Singleton);
        }

        static async Task<int> Main(string[] args)
        {
            var program = new Program();

            //return await program.StartManually(args);

            return await program.StartAutomatically(args);
        }

        public async Task<int> StartManually(string[] args)
        {
            // Set factory
            var creationMethod = (Type commandType) =>
            {
                return container.GetInstance(commandType) as IStarterCommand;
            };
            Starter.SetFactory(creationMethod);

            // Find commands
            starter.FindCommandsTypes();

            // Place the code here because I should iterate through all commands
            container.Register<CmdService>(Lifestyle.Singleton);

            return await starter.Start(args);
        }

        public async Task<int> StartAutomatically(string[] args)
        {
            return await starter.Start(this, args);
        }

        public object? GetService(Type serviceType)
        {
            return container.GetInstance(serviceType);
        }

        public void SetService(Type service)
        {
            container.Register(service, service, Lifestyle.Singleton);
        }
    }
}