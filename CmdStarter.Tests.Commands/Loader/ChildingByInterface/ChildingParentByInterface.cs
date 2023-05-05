namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.ChildingByInterface
{
    public class ChildingParentByInterface : IStarterCommand
    {
        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public Delegate HandlingMethod => () => { };

        public static IStarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand
            => new ChildingParentByInterface();
    }
}
