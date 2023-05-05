namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.ChildingByInterface.Children
{
    [Parent<ChildingParentByInterface>]
    public class ChildingChildByInterface : IStarterCommand
    {
        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public Delegate HandlingMethod => () => { };

        public static IStarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand
            => new ChildingChildByInterface();
    }
}
