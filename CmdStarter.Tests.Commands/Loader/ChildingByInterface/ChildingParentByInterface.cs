namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.ChildingByInterface
{
    public class ChildingParentByInterface : IStarterCommand
    {
        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public Delegate HandlingMethod => IStarterCommand.EMPTY_EXECUTION;
    }
}
