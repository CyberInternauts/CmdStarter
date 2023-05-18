using com.cyberinternauts.csharp.CmdStarter.Lib.Interfaces;

namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.ByInterface
{
    /// <summary>
    /// A second test case to ensure the usage of the same class <see cref="Lib.SpecialCommands.GenericStarterCommand{CommandType}"/> 
    /// (As all commands detected by interface use this generic class)
    /// </summary>
    public class SecondByInterface : IStarterCommand
    {
        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public Delegate HandlingMethod => IStarterCommand.EMPTY_EXECUTION;
    }
}
