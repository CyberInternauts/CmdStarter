using CmdStarter.Demo.SimpleInjector.Services;
using com.cyberinternauts.csharp.CmdStarter.Lib;
using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace CmdStarter.Demo.SimpleInjector.Commands
{
    internal class CmdService : IStarterCommand
    {
        private IService service;

        public int MyInt { get; set; } = 111;

        public CmdService(IService service)
        {
            this.service = service;
        }

        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public Delegate HandlingMethod => () => { 
            Console.WriteLine(service.GetType().Name + "=" + service.GetInt());
            Console.WriteLine(nameof(MyInt) + "=" + MyInt);
        };
    }
}
