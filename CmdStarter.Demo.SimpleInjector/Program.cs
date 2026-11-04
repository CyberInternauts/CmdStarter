// Demo: an alternative container (SimpleInjector) plugged in through Starter.SetFactory.
using CyberInternauts.CmdStarter.Demo.SimpleInjector.Services;
using CyberInternauts.CmdStarter.Interfaces;
using SimpleInjector;

namespace CyberInternauts.CmdStarter.Demo.SimpleInjector;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var starter = new Starter();
        var container = new Container();
        container.Register<IService, Service2>(Lifestyle.Singleton);

        starter.FindCommandsTypes();
        foreach (var commandType in starter.CommandsTypes)
        {
            container.Register(commandType);
        }

        starter.SetFactory(type => (IStarterCommand)container.GetInstance(type));
        return await starter.Start(args);
    }
}
