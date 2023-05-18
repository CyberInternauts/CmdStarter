namespace com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.ByInterface
{
    public class ConstructorWithParams : IStarterCommand
    {
        public GlobalOptionsManager? GlobalOptionsManager { get; set; }

        public Delegate HandlingMethod => IStarterCommand.EMPTY_EXECUTION;

        public static IStarterCommand GetInstance<CommandType>() where CommandType : IStarterCommand
        {
            return new ConstructorWithParams(42);
        }

        public ConstructorWithParams(int testParam)
        {
        }
    }
}
