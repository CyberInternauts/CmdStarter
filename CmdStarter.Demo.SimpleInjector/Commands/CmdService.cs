using CyberInternauts.CmdStarter.Demo.SimpleInjector.Services;
using CyberInternauts.CmdStarter;
using CyberInternauts.CmdStarter.Interfaces;

namespace CyberInternauts.CmdStarter.Demo.SimpleInjector.Commands;

internal class CmdService : IStarterCommand
{
    private IService service;

    public int MyInt { get; set; } = 111;

    public CmdService(IService service)
    {
        this.service = service;
    }

    public GlobalOptionsManager? GlobalOptionsManager { get; set; }

    public Delegate HandlingMethod => () =>
    {
        Console.WriteLine(service.GetType().Name + "=" + service.GetInt());
        Console.WriteLine(nameof(MyInt) + "=" + MyInt);
    };
}
