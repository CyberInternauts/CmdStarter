using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.ByInterface
{
    public class FirstByInterface : IStarterCommand
    {
        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public Delegate HandlingMethod => IStarterCommand.EMPTY_EXECUTION;

    }
}
